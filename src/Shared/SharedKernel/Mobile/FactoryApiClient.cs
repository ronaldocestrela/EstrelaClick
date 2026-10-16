using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Shared.SharedKernel.Mobile;

public sealed class FactoryApiClient : IFactoryApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IOperatorSessionService _sessionService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FactoryApiClient(HttpClient httpClient, IOperatorSessionService sessionService)
    {
        _httpClient = httpClient;
        _sessionService = sessionService;
    }

    public async Task<Result<OperadorLoginResult>> LoginAsync(string login, string senha, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            return Result<OperadorLoginResult>.Failure(MobileErrors.DadosObrigatorios);
        }

        try
        {
            var payload = new { Login = login.Trim(), Senha = senha.Trim() };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/operador-login", content, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<OperadorLoginResult>.Failure(MobileErrors.CredenciaisInvalidas);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<OperadorLoginResult>.Failure(MobileErrors.FalhaComunicacao);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OperadorLoginResult>(json, JsonOptions);

            if (result is null)
            {
                return Result<OperadorLoginResult>.Failure(MobileErrors.RespostaInvalida);
            }

            return Result<OperadorLoginResult>.Success(result);
        }
        catch (HttpRequestException)
        {
            return Result<OperadorLoginResult>.Failure(MobileErrors.FalhaComunicacao);
        }
    }

    public async Task<Result<PlacaAtivaInfo>> ObterProximaPendenteAsync(string? numeroLote = null, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/placas/proxima-pendente{(string.IsNullOrWhiteSpace(numeroLote) ? "" : $"?numeroLote={Uri.EscapeDataString(numeroLote.Trim())}")}");
            ApplyAuthenticationHeader(request);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<PlacaAtivaInfo>.Failure(MobileErrors.NaoAutorizado);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<PlacaAtivaInfo>.Failure(MobileErrors.FilaVazia);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<PlacaAtivaInfo>.Failure(MobileErrors.FalhaComunicacao);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PlacaAtivaInfo>(json, JsonOptions);

            if (result is null)
            {
                return Result<PlacaAtivaInfo>.Failure(MobileErrors.RespostaInvalida);
            }

            return Result<PlacaAtivaInfo>.Success(result);
        }
        catch (HttpRequestException)
        {
            return Result<PlacaAtivaInfo>.Failure(MobileErrors.FalhaComunicacao);
        }
    }

    public async Task<Result<ResumoLoteInfo>> ObterResumoLoteAsync(string numeroLote, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(numeroLote))
        {
            return Result<ResumoLoteInfo>.Failure(new Error("mobile.lote.invalido", "Número de lote é obrigatório."));
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/placas/lotes/{Uri.EscapeDataString(numeroLote.Trim())}/resumo");
            ApplyAuthenticationHeader(request);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<ResumoLoteInfo>.Failure(MobileErrors.NaoAutorizado);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result<ResumoLoteInfo>.Failure(MobileErrors.FilaVazia);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<ResumoLoteInfo>.Failure(MobileErrors.FalhaComunicacao);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ResumoLoteInfo>(json, JsonOptions);

            if (result is null)
            {
                return Result<ResumoLoteInfo>.Failure(MobileErrors.RespostaInvalida);
            }

            return Result<ResumoLoteInfo>.Success(result);
        }
        catch (HttpRequestException)
        {
            return Result<ResumoLoteInfo>.Failure(MobileErrors.FalhaComunicacao);
        }
    }

    public async Task<Result<ConfirmacaoGravacaoResult>> ConfirmarGravacaoAsync(string slug, string chipUid, CancellationToken ct = default)
    {
        try
        {
            var payload = new { Slug = slug.Trim(), ChipUid = chipUid.Trim() };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/placas/confirmar-gravacao")
            {
                Content = content
            };
            ApplyAuthenticationHeader(request);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.NaoAutorizado);
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.FalhaComunicacao);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ConfirmacaoGravacaoResult>(json, JsonOptions);

            if (result is null)
            {
                return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.RespostaInvalida);
            }

            return Result<ConfirmacaoGravacaoResult>.Success(result);
        }
        catch (HttpRequestException)
        {
            return Result<ConfirmacaoGravacaoResult>.Failure(MobileErrors.FalhaComunicacao);
        }
    }

    private void ApplyAuthenticationHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_sessionService.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionService.Token);
        }
    }
}
