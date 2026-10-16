using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Configuration;

/// <summary>
/// Utilitário centralizado e resiliente para carregamento de variáveis de ambiente a partir de arquivos '.env'.
/// Atende às regras do AGENTS.md com zero dependências externas e retorno estrito via Result&lt;T&gt;.
/// </summary>
public static class DotEnv
{
    private const string DefaultFileName = ".env";

    /// <summary>
    /// Analisa o conteúdo em texto bruto de um arquivo .env e retorna um dicionário chave/valor.
    /// </summary>
    public static Result<IReadOnlyDictionary<string, string>> Parse(string? content)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(content))
        {
            return Result<IReadOnlyDictionary<string, string>>.Success(dictionary);
        }

        using var reader = new StringReader(content);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();

            // Ignora linhas vazias e comentários
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Suporta prefixo 'export ' estilo bash
            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line["export ".Length..].TrimStart();
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = line[..equalsIndex].Trim();
            var rawValue = line[(equalsIndex + 1)..].Trim();

            var parsedValue = SanitizeValue(rawValue);
            dictionary[key] = parsedValue;
        }

        return Result<IReadOnlyDictionary<string, string>>.Success(dictionary);
    }

    /// <summary>
    /// Carrega as variáveis de ambiente a partir de um arquivo .env especificado ou localiza automaticamente na árvore de diretórios.
    /// </summary>
    /// <param name="filePath">Caminho específico do arquivo .env. Se nulo, pesquisa a partir do diretório de execução atual.</param>
    /// <param name="optional">Se verdadeiro, a ausência do arquivo não é tratada como erro, retornando sucesso com 0 variáveis.</param>
    /// <param name="overrideExisting">Se verdadeiro, sobrescreve variáveis de ambiente já definidas no processo.</param>
    public static Result<int> Load(string? filePath = null, bool optional = true, bool overrideExisting = false)
    {
        string? resolvedPath = filePath;

        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            resolvedPath = FindEnvFile(Directory.GetCurrentDirectory(), DefaultFileName);
        }

        if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
        {
            if (optional)
            {
                return Result<int>.Success(0);
            }

            return Result<int>.Failure(new Error("DotEnv.FileNotFound", $"Arquivo de ambiente '{filePath ?? DefaultFileName}' não encontrado."));
        }

        string fileContent;
        try
        {
            fileContent = File.ReadAllText(resolvedPath);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(new Error("DotEnv.ReadError", $"Falha ao ler arquivo .env: {ex.Message}"));
        }

        var parseResult = Parse(fileContent);
        if (parseResult.IsFailure)
        {
            return Result<int>.Failure(parseResult.Error);
        }

        var variables = parseResult.Value!;
        var loadedCount = 0;

        foreach (var (key, value) in variables)
        {
            var wasSet = SetProcessEnvironmentVariable(key, value, overrideExisting);
            if (wasSet)
            {
                loadedCount++;
            }

            // Garante interoperabilidade com o provedor de configuração do ASP.NET Core
            // Mapeando ':' para '__' e vice-versa
            if (key.Contains(':'))
            {
                var normalizedKey = key.Replace(":", "__");
                SetProcessEnvironmentVariable(normalizedKey, value, overrideExisting);
            }
            else if (key.Contains("__"))
            {
                var colonKey = key.Replace("__", ":");
                SetProcessEnvironmentVariable(colonKey, value, overrideExisting);
            }
        }

        return Result<int>.Success(loadedCount);
    }

    /// <summary>
    /// Carrega as variáveis de ambiente iniciando a busca recursiva a partir de um diretório base.
    /// </summary>
    public static Result<int> LoadFromDirectory(string directory, string fileName = DefaultFileName, bool optional = true, bool overrideExisting = false)
    {
        var resolvedPath = FindEnvFile(directory, fileName);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            if (optional)
            {
                return Result<int>.Success(0);
            }

            return Result<int>.Failure(new Error("DotEnv.FileNotFound", $"Arquivo '{fileName}' não encontrado no diretório '{directory}' ou ancestrais."));
        }

        return Load(resolvedPath, optional, overrideExisting);
    }

    /// <summary>
    /// Procura recursivamente pelo arquivo de ambiente no diretório fornecido e em seus diretórios pai.
    /// </summary>
    public static string? FindEnvFile(string startDirectory, string fileName = DefaultFileName)
    {
        if (string.IsNullOrWhiteSpace(startDirectory) || !Directory.Exists(startDirectory))
        {
            return null;
        }

        var currentDir = new DirectoryInfo(startDirectory);
        const int maxParentLevels = 10;
        var levels = 0;

        while (currentDir is not null && levels < maxParentLevels)
        {
            var candidate = Path.Combine(currentDir.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            currentDir = currentDir.Parent;
            levels++;
        }

        return null;
    }

    private static bool SetProcessEnvironmentVariable(string key, string value, bool overrideExisting)
    {
        var existing = Environment.GetEnvironmentVariable(key);
        if (overrideExisting || string.IsNullOrEmpty(existing))
        {
            Environment.SetEnvironmentVariable(key, value);
            return true;
        }

        return false;
    }

    private static string SanitizeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Aspas duplas: remove e processa escapes
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            var unwrapped = value[1..^1];
            return unwrapped
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        // Aspas simples: remove sem interpretar escapes
        if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
        {
            return value[1..^1];
        }

        return value;
    }
}
