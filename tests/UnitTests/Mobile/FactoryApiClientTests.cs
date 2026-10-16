using System.Net;
using System.Text.Json;
using EstrelaClick.Shared.SharedKernel.Mobile;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class FactoryApiClientTests
{
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    private readonly IOperatorSessionService _sessionService = new OperatorSessionService();

    [Fact]
    public async Task LoginAsync_QuandoSucesso_DeveRetornarOperadorLoginResult()
    {
        // Arrange
        var expectedResult = new OperadorLoginResult("token-jwt-teste", "OP01", "Lucas", DateTime.UtcNow.AddHours(4));
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/auth/operador-login");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResult))
            };
        });

        var client = new FactoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _sessionService);

        // Act
        var result = await client.LoginAsync("OP01", "123456");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be("token-jwt-teste");
        result.Value.OperadorId.Should().Be("OP01");
    }

    [Fact]
    public async Task LoginAsync_QuandoCredenciaisInvalidas_DeveRetornarFalhaCredenciaisInvalidas()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = new FactoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _sessionService);

        // Act
        var result = await client.LoginAsync("OP01", "senha-errada");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MobileErrors.CredenciaisInvalidas);
    }

    [Fact]
    public async Task ObterProximaPendenteAsync_QuandoFilaVazia_DeveRetornarFalhaFilaVazia()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = new FactoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _sessionService);

        // Act
        var result = await client.ObterProximaPendenteAsync("LOTE-01");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MobileErrors.FilaVazia);
    }

    [Fact]
    public async Task ObterProximaPendenteAsync_QuandoSucesso_DeveRetornarPlacaAtivaInfo()
    {
        // Arrange
        var expectedPlaca = new PlacaAtivaInfo(
            Guid.NewGuid(),
            "0123456789abcdef0123456789abcdef",
            "LOTE-2026",
            "https://estrelaclick.com/r/0123456789abcdef0123456789abcdef",
            DateTime.UtcNow,
            DateTime.UtcNow.AddSeconds(90),
            "OP01");

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(expectedPlaca))
        });

        var client = new FactoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _sessionService);

        // Act
        var result = await client.ObterProximaPendenteAsync("LOTE-2026");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Slug.Should().Be("0123456789abcdef0123456789abcdef");
        result.Value.NumeroLote.Should().Be("LOTE-2026");
    }

    [Fact]
    public async Task ObterResumoLoteAsync_QuandoSucesso_DeveRetornarResumoLoteInfo()
    {
        // Arrange
        var expectedResumo = new ResumoLoteInfo("LOTE-01", 100, 45, 55, 0);
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/placas/lotes/LOTE-01/resumo");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResumo))
            };
        });

        var client = new FactoryApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }, _sessionService);

        // Act
        var result = await client.ObterResumoLoteAsync("LOTE-01");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalPlacas.Should().Be(100);
        result.Value.TotalGravadas.Should().Be(45);
        result.Value.TotalPendentes.Should().Be(55);
    }
}
