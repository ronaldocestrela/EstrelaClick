using System.Net;
using System.Text.Json;
using EstrelaClick.Frontend.AdminDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.UI;

public class AdminApiClientTests
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

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ObterMetricasFabricaAsync_QuandoSucesso_DeveRetornarMetricas()
    {
        // Arrange
        var metricas = new MetricasFabricaDto(100, 40, 10, 30, 20, 5);
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/admin/metricas-fabrica");
            req.Headers.Contains("X-Api-Key").Should().BeTrue();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(metricas, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.ObterMetricasFabricaAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(metricas);
    }

    [Fact]
    public async Task ListarLotesAsync_QuandoSucesso_DeveRetornarListaDeLotes()
    {
        // Arrange
        var lotes = new List<LoteItemDto>
        {
            new("LOTE-01", 50, 10, 5, 20, 15, DateTime.UtcNow)
        };

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/admin/lotes");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(lotes, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.ListarLotesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].NumeroLote.Should().Be("LOTE-01");
    }

    [Fact]
    public async Task GerarLoteAsync_QuandoSucesso_DeveEnviarComandoERetornarLoteCriado()
    {
        // Arrange
        var loteCriado = new LoteCriadoDto("LOTE-TESTE", 5, DateTime.UtcNow, new[] { "slug1", "slug2" });
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/placas/gerar-lote");
            req.Method.Should().Be(HttpMethod.Post);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(loteCriado, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.GerarLoteAsync("LOTE-TESTE", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.NumeroLote.Should().Be("LOTE-TESTE");
        result.Value.QuantidadeCriada.Should().Be(5);
    }

    [Fact]
    public async Task ConsultarAuditoriaAsync_QuandoEncontrada_DeveRetornarPlacaAuditoria()
    {
        // Arrange
        var auditoria = new PlacaAuditoriaDto(
            Guid.NewGuid(), "slug1234567890123456789012345678", "LOTE-01", StatusPlaca.Gravada,
            "04:A1:B2:C3", "https://google.com", null, null, "Op1",
            DateTime.UtcNow, DateTime.UtcNow, null, 10
        );

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Contain("/api/admin/placas/auditoria?termo=slug123");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(auditoria, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.ConsultarAuditoriaAsync("slug123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ChipUid.Should().Be("04:A1:B2:C3");
        result.Value.TotalAcessos.Should().Be(10);
    }

    [Fact]
    public async Task ListarEmpresasAsync_QuandoSucesso_DeveRetornarEmpresas()
    {
        // Arrange
        var empresas = new List<EmpresaDto>
        {
            new(Guid.NewGuid(), "Padaria Estrela", "padaria@estrela.com", "(11) 9999-8888", true, DateTime.UtcNow, 3)
        };

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/admin/empresas");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(empresas, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.ListarEmpresasAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].RazaoSocial.Should().Be("Padaria Estrela");
    }

    [Fact]
    public async Task CadastrarEmpresaAsync_QuandoSucesso_DeveRetornarEmpresaCriada()
    {
        // Arrange
        var novaEmpresa = new EmpresaDto(Guid.NewGuid(), "Bar Estrela", "bar@estrela.com", "(11) 8888-7777", true, DateTime.UtcNow, 0);

        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.PathAndQuery.Should().Be("/api/admin/empresas");
            req.Method.Should().Be(HttpMethod.Post);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(novaEmpresa, _jsonOptions))
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var client = new AdminApiClient(httpClient, "test-api-key");

        // Act
        var result = await client.CadastrarEmpresaAsync(new CriarEmpresaRequest("Bar Estrela", "bar@estrela.com", "(11) 8888-7777"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RazaoSocial.Should().Be("Bar Estrela");
    }
}
