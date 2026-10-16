using System.Net.Http.Json;
using System.Text.Json;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Frontend.AdminDashboard.Services;

public interface IAdminApiClient
{
    Task<Result<MetricasFabricaDto>> ObterMetricasFabricaAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LoteItemDto>>> ListarLotesAsync(CancellationToken cancellationToken = default);
    Task<Result<LoteCriadoDto>> GerarLoteAsync(string numeroLote, int quantidade, CancellationToken cancellationToken = default);
    Task<Result<PlacaAuditoriaDto>> ConsultarAuditoriaAsync(string termo, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EmpresaDto>>> ListarEmpresasAsync(CancellationToken cancellationToken = default);
    Task<Result<EmpresaDto>> CadastrarEmpresaAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default);
    string ObterUrlDownloadZipLote(string numeroLote);
}

public sealed class AdminApiClient : IAdminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public AdminApiClient(HttpClient httpClient, string? apiKey = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<Result<MetricasFabricaDto>> ObterMetricasFabricaAsync(CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Get, "/api/admin/metricas-fabrica");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<MetricasFabricaDto>(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<LoteItemDto>>> ListarLotesAsync(CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Get, "/api/admin/lotes");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<IReadOnlyList<LoteItemDto>>(response, cancellationToken);
    }

    public async Task<Result<LoteCriadoDto>> GerarLoteAsync(string numeroLote, int quantidade, CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Post, "/api/placas/gerar-lote");
        request.Content = JsonContent.Create(new GerarLoteRequest(numeroLote, quantidade));
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<LoteCriadoDto>(response, cancellationToken);
    }

    public async Task<Result<PlacaAuditoriaDto>> ConsultarAuditoriaAsync(string termo, CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Get, $"/api/admin/placas/auditoria?termo={Uri.EscapeDataString(termo)}");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<PlacaAuditoriaDto>(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<EmpresaDto>>> ListarEmpresasAsync(CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Get, "/api/admin/empresas");
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<IReadOnlyList<EmpresaDto>>(response, cancellationToken);
    }

    public async Task<Result<EmpresaDto>> CadastrarEmpresaAsync(CriarEmpresaRequest novoCliente, CancellationToken cancellationToken = default)
    {
        using var request = CriarRequest(HttpMethod.Post, "/api/admin/empresas");
        request.Content = JsonContent.Create(novoCliente);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        return await TratarRespostaAsync<EmpresaDto>(response, cancellationToken);
    }

    public string ObterUrlDownloadZipLote(string numeroLote)
    {
        var baseAddress = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        return $"{baseAddress}/api/placas/lotes/{Uri.EscapeDataString(numeroLote)}/exportar-zip";
    }

    private HttpRequestMessage CriarRequest(HttpMethod metodo, string url)
    {
        var request = new HttpRequestMessage(metodo, url);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Add("X-Api-Key", _apiKey);
        }
        return request;
    }

    private async Task<Result<T>> TratarRespostaAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            if (content is not null)
            {
                return Result<T>.Success(content);
            }
            return Result<T>.Failure(new Error("api.resposta.vazia", "Resposta da API retornou conteúdo nulo."));
        }

        try
        {
            var errorDoc = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions, cancellationToken);
            var detail = errorDoc.TryGetProperty("detail", out var d) ? d.GetString() : "Erro ao processar requisição na API.";
            var code = errorDoc.TryGetProperty("errorCode", out var c) ? c.GetString() : $"http.{response.StatusCode}";

            return Result<T>.Failure(new Error(code ?? "api.erro", detail ?? "Falha na comunicação com a API."));
        }
        catch
        {
            return Result<T>.Failure(new Error($"http.status.{(int)response.StatusCode}", $"Falha HTTP: {response.StatusCode}"));
        }
    }
}
