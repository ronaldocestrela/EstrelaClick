using System.Net.Http.Json;
using System.Text.Json;
using EstrelaClick.Shared.SharedKernel.Client;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Frontend.ClientDashboard.Services;

public interface IClientApiClient
{
    Task<Result<VerificarOnboardingDto>> VerificarOnboardingAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<PlacaAtivadaDto>> AtivarOnboardingAsync(AtivarPlacaRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<PlacaItemDto>>> ListarPlacasAsync(Guid empresaId, CancellationToken cancellationToken = default);
    Task<Result> AtualizarUrlAsync(Guid empresaId, AtualizarUrlRequest request, CancellationToken cancellationToken = default);
    Task<Result> ConfigurarFiltroAsync(Guid empresaId, ConfigurarFiltroRequest request, CancellationToken cancellationToken = default);
    Task<Result<AnalyticsDto>> ObterAnalyticsAsync(Guid empresaId, Guid? placaId = null, int dias = 30, CancellationToken cancellationToken = default);
}

public sealed class ClientApiClient : IClientApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ClientApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<VerificarOnboardingDto>> VerificarOnboardingAsync(string slug, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/client/onboarding/verificar/{Uri.EscapeDataString(slug)}", cancellationToken);
        return await TratarRespostaAsync<VerificarOnboardingDto>(response, cancellationToken);
    }

    public async Task<Result<PlacaAtivadaDto>> AtivarOnboardingAsync(AtivarPlacaRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/client/onboarding/ativar", request, cancellationToken);
        return await TratarRespostaAsync<PlacaAtivadaDto>(response, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<PlacaItemDto>>> ListarPlacasAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/client/empresas/{empresaId}/placas", cancellationToken);
        return await TratarRespostaAsync<IReadOnlyList<PlacaItemDto>>(response, cancellationToken);
    }

    public async Task<Result> AtualizarUrlAsync(Guid empresaId, AtualizarUrlRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/client/empresas/{empresaId}/placas/{request.PlacaId}/url", request, cancellationToken);
        return await TratarRespostaVaziaAsync(response, cancellationToken);
    }

    public async Task<Result> ConfigurarFiltroAsync(Guid empresaId, ConfigurarFiltroRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/client/empresas/{empresaId}/placas/{request.PlacaId}/filtro-satisfacao", request, cancellationToken);
        return await TratarRespostaVaziaAsync(response, cancellationToken);
    }

    public async Task<Result<AnalyticsDto>> ObterAnalyticsAsync(Guid empresaId, Guid? placaId = null, int dias = 30, CancellationToken cancellationToken = default)
    {
        var query = $"/api/client/empresas/{empresaId}/analytics?dias={dias}";
        if (placaId.HasValue && placaId.Value != Guid.Empty)
        {
            query += $"&placaId={placaId.Value}";
        }

        var response = await _httpClient.GetAsync(query, cancellationToken);
        return await TratarRespostaAsync<AnalyticsDto>(response, cancellationToken);
    }

    private async Task<Result<T>> TratarRespostaAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
            return content is not null
                ? Result<T>.Success(content)
                : Result<T>.Failure(new Error("client.resposta.nula", "Resposta do servidor vazia."));
        }

        var erro = await ExtrairErroAsync(response, cancellationToken);
        return Result<T>.Failure(erro);
    }

    private async Task<Result> TratarRespostaVaziaAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        var erro = await ExtrairErroAsync(response, cancellationToken);
        return Result.Failure(erro);
    }

    private async Task<Error> ExtrairErroAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions, cancellationToken);
            var code = problem.TryGetProperty("extensions", out var ext) && ext.TryGetProperty("errorCode", out var ec)
                ? ec.GetString() ?? $"HTTP_{response.StatusCode}"
                : $"HTTP_{response.StatusCode}";

            var detail = problem.TryGetProperty("detail", out var dt)
                ? dt.GetString() ?? response.ReasonPhrase ?? "Erro ao processar solicitação."
                : response.ReasonPhrase ?? "Erro ao processar solicitação.";

            return new Error(code, detail);
        }
        catch
        {
            return new Error($"HTTP_{response.StatusCode}", response.ReasonPhrase ?? "Falha na comunicação com o servidor.");
        }
    }
}
