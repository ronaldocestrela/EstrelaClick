using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EstrelaClick.Backend.Infrastructure.Security;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var extractedApiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var extractedApiKey = extractedApiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(extractedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Chave de API não informada."));
        }

        var configuredApiKey = _configuration["FactorySecurity:ApiKey"] ?? "dev-factory-secret-key-2026";

        if (!string.Equals(extractedApiKey.Trim(), configuredApiKey.Trim(), StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Chave de API inválida."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "FactoryClient"),
            new Claim(ClaimTypes.Name, "Factory Operator / Machine"),
            new Claim(ClaimTypes.Role, "Factory")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
