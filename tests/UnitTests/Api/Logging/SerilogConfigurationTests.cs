using EstrelaClick.Backend.Api.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Api.Logging;

public class SerilogConfigurationTests
{
    [Fact]
    public void BuildLoggerConfiguration_ComConfiguracaoBasica_DeveRetornarLoggerValido()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Serilog:MinimumLevel:Default", "Information" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:Properties:Application", "EstrelaClick.Backend.Api" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Testing");

        // Act
        var loggerConfig = SerilogConfigurationExtensions.CreateLoggerConfiguration(configuration, environment);
        using var logger = loggerConfig.CreateLogger();

        // Assert
        logger.Should().NotBeNull();
        logger.IsEnabled(LogEventLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void EnrichDiagnosticContext_DevePreencherPropriedadesRequisicaoHttp()
    {
        // Arrange
        var diagnosticContext = Substitute.For<IDiagnosticContext>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/r/slug-teste";
        httpContext.Request.Host = new HostString("estrela.click");
        httpContext.Request.Scheme = "https";
        httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 TestBrowser";
        httpContext.TraceIdentifier = "trace-req-123";

        // Act
        SerilogConfigurationExtensions.EnrichDiagnosticContext(diagnosticContext, httpContext);

        // Assert
        diagnosticContext.Received().Set("RequestHost", "estrela.click");
        diagnosticContext.Received().Set("RequestScheme", "https");
        diagnosticContext.Received().Set("UserAgent", "Mozilla/5.0 TestBrowser");
        diagnosticContext.Received().Set("TraceIdentifier", "trace-req-123");
    }

    [Fact]
    public void EnrichDiagnosticContext_QuandoUsuarioAutenticado_DeveIncluirUserId()
    {
        // Arrange
        var diagnosticContext = Substitute.For<IDiagnosticContext>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/placas";
        httpContext.TraceIdentifier = "trace-auth-456";

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "user-guid-999")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        httpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);

        // Act
        SerilogConfigurationExtensions.EnrichDiagnosticContext(diagnosticContext, httpContext);

        // Assert
        diagnosticContext.Received().Set("UserId", "user-guid-999");
    }
}
