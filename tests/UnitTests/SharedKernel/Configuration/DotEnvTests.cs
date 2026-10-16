using EstrelaClick.Shared.SharedKernel.Configuration;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.SharedKernel.Configuration;

public class DotEnvTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _envKeysToClean = new();

    public DotEnvTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "DotEnvTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        foreach (var key in _envKeysToClean)
        {
            Environment.SetEnvironmentVariable(key, null);
        }

        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignora falha de limpeza de temp em teste
            }
        }
    }

    private void TrackEnvVar(string key)
    {
        _envKeysToClean.Add(key);
    }

    [Fact]
    public void Parse_ComEntradasValidas_DeveMapearChaveEValorCorretamente()
    {
        var content = """
            DB_HOST=localhost
            DB_PORT=1433
            APP_NAME=EstrelaClick
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("DB_HOST").WhoseValue.Should().Be("localhost");
        result.Value.Should().ContainKey("DB_PORT").WhoseValue.Should().Be("1433");
        result.Value.Should().ContainKey("APP_NAME").WhoseValue.Should().Be("EstrelaClick");
    }

    [Fact]
    public void Parse_ComEspacosELinhasVazias_DeveIgnorarETrimar()
    {
        var content = """
            
               KEY1=value1   
            
               KEY2 = value2
            
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!["KEY1"].Should().Be("value1");
        result.Value!["KEY2"].Should().Be("value2");
    }

    [Fact]
    public void Parse_ComComentarios_DeveIgnorarLinhasComHashtag()
    {
        var content = """
            # Este é um comentário
               # Outro comentário com espaço
            VALID_KEY=valid_value
            # DESABILITADO=123
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainKey("VALID_KEY").WhoseValue.Should().Be("valid_value");
    }

    [Fact]
    public void Parse_ComPrefixoExport_DeveRemoverExport()
    {
        var content = """
            export API_KEY=secret_123
            export   DB_USER=sa
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("API_KEY").WhoseValue.Should().Be("secret_123");
        result.Value.Should().ContainKey("DB_USER").WhoseValue.Should().Be("sa");
    }

    [Fact]
    public void Parse_ComAspasSimplesEDuplas_DeveRemoverAspas()
    {
        var content = """
            SINGLE_QUOTED='valor com espaços e aspas'
            DOUBLE_QUOTED="outro valor com espaços"
            NO_QUOTES=valor_simples
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value!["SINGLE_QUOTED"].Should().Be("valor com espaços e aspas");
        result.Value!["DOUBLE_QUOTED"].Should().Be("outro valor com espaços");
        result.Value!["NO_QUOTES"].Should().Be("valor_simples");
    }

    [Fact]
    public void Parse_ComValoresContendoSinalDeIgual_DevePreservarValorInteiro()
    {
        var content = """
            CONNECTION_STRING=Server=localhost,1433;Database=test;User Id=sa;Password=Pass=123;
            EQUALS_VALUE=a=b=c=d
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value!["CONNECTION_STRING"].Should().Be("Server=localhost,1433;Database=test;User Id=sa;Password=Pass=123;");
        result.Value!["EQUALS_VALUE"].Should().Be("a=b=c=d");
    }

    [Fact]
    public void Parse_ComEscapeCharacters_DeveTratarQuebrasDeLinhaETab()
    {
        var content = """
            MULTILINE="linha1\nlinha2\tcomtab"
            """;

        var result = DotEnv.Parse(content);

        result.IsSuccess.Should().BeTrue();
        result.Value!["MULTILINE"].Should().Be("linha1\nlinha2\tcomtab");
    }

    [Fact]
    public void Load_QuandoArquivoNaoExisteEOpcional_DeveRetornarSucessoComZeroVariaveis()
    {
        var nonExistentFile = Path.Combine(_tempDirectory, "non_existent.env");

        var result = DotEnv.Load(nonExistentFile, optional: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Load_QuandoArquivoNaoExisteEExigido_DeveRetornarFalhaComErro()
    {
        var nonExistentFile = Path.Combine(_tempDirectory, "non_existent.env");

        var result = DotEnv.Load(nonExistentFile, optional: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DotEnv.FileNotFound");
    }

    [Fact]
    public void Load_ComArquivoValido_DeveDefinirVariaveisDeAmbienteNoProcesso()
    {
        var key = "TEST_ENV_VAR_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);

        var filePath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(filePath, $"{key}=hello_estrela");

        var result = DotEnv.Load(filePath);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(1);
        Environment.GetEnvironmentVariable(key).Should().Be("hello_estrela");
    }

    [Fact]
    public void Load_ComChavesComDoisPontos_DeveDefinirTantoComDoisPontosQuantoComSublinhadoDuplo()
    {
        var baseKey = "TestConfig" + Guid.NewGuid().ToString("N");
        var colonKey = $"{baseKey}:SubKey";
        var doubleUnderscoreKey = $"{baseKey}__SubKey";

        TrackEnvVar(colonKey);
        TrackEnvVar(doubleUnderscoreKey);

        var filePath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(filePath, $"{colonKey}=nested_value");

        var result = DotEnv.Load(filePath);

        result.IsSuccess.Should().BeTrue();
        Environment.GetEnvironmentVariable(colonKey).Should().Be("nested_value");
        Environment.GetEnvironmentVariable(doubleUnderscoreKey).Should().Be("nested_value");
    }

    [Fact]
    public void Load_QuandoOverrideExistingForFalse_NaoDeveSobrescreverVariavelJaExistente()
    {
        var key = "TEST_OVERRIDE_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);
        Environment.SetEnvironmentVariable(key, "original_value");

        var filePath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(filePath, $"{key}=new_value");

        var result = DotEnv.Load(filePath, overrideExisting: false);

        result.IsSuccess.Should().BeTrue();
        Environment.GetEnvironmentVariable(key).Should().Be("original_value");
    }

    [Fact]
    public void Load_QuandoOverrideExistingForTrue_DeveSobrescreverVariavelExistente()
    {
        var key = "TEST_OVERRIDE_TRUE_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);
        Environment.SetEnvironmentVariable(key, "original_value");

        var filePath = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(filePath, $"{key}=new_value");

        var result = DotEnv.Load(filePath, overrideExisting: true);

        result.IsSuccess.Should().BeTrue();
        Environment.GetEnvironmentVariable(key).Should().Be("new_value");
    }

    [Fact]
    public void Load_BuscaRecursivaParaCima_DeveEncontrarArquivoNoDiretorioPai()
    {
        var key = "TEST_PARENT_LOOKUP_" + Guid.NewGuid().ToString("N");
        TrackEnvVar(key);

        var parentEnv = Path.Combine(_tempDirectory, ".env");
        File.WriteAllText(parentEnv, $"{key}=from_parent");

        var subDir = Path.Combine(_tempDirectory, "subdir", "child");
        Directory.CreateDirectory(subDir);

        var result = DotEnv.LoadFromDirectory(subDir);

        result.IsSuccess.Should().BeTrue();
        Environment.GetEnvironmentVariable(key).Should().Be("from_parent");
    }
}
