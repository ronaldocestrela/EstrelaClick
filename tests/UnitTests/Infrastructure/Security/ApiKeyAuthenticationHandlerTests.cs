using System.Security.Claims;
using System.Text.Encodings.Web;
using EstrelaClick.Backend.Infrastructure.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.Security;

public class ApiKeyAuthenticationHandlerTests
{
    private readonly IOptionsMonitor<ApiKeyAuthenticationOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly UrlEncoder _urlEncoder;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandlerTests()
    {
        _optionsMonitor = Substitute.For<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        _optionsMonitor.Get(ApiKeyAuthenticationOptions.DefaultScheme).Returns(new ApiKeyAuthenticationOptions());

        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _urlEncoder = UrlEncoder.Default;

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "FactorySecurity:ApiKey", "segredo-fabrica-teste-123" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_DeveRetornarNoResult_QuandoHeaderNaoEstiverPresente()
    {
        var handler = new ApiKeyAuthenticationHandler(_optionsMonitor, _loggerFactory, _urlEncoder, _configuration);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(new AuthenticationScheme(ApiKeyAuthenticationOptions.DefaultScheme, null, typeof(ApiKeyAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_DeveRetornarFalha_QuandoHeaderForInvalido()
    {
        var handler = new ApiKeyAuthenticationHandler(_optionsMonitor, _loggerFactory, _urlEncoder, _configuration);
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyAuthenticationOptions.HeaderName] = "chave-errada";
        await handler.InitializeAsync(new AuthenticationScheme(ApiKeyAuthenticationOptions.DefaultScheme, null, typeof(ApiKeyAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should().Be("Chave de API inválida.");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_DeveRetornarSucesso_QuandoHeaderForValido()
    {
        var handler = new ApiKeyAuthenticationHandler(_optionsMonitor, _loggerFactory, _urlEncoder, _configuration);
        var context = new DefaultHttpContext();
        context.Request.Headers[ApiKeyAuthenticationOptions.HeaderName] = "segredo-fabrica-teste-123";
        await handler.InitializeAsync(new AuthenticationScheme(ApiKeyAuthenticationOptions.DefaultScheme, null, typeof(ApiKeyAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.IsInRole("Factory").Should().BeTrue();
    }
}
