using System.Net;
using System.Net.Http.Json;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Backend.Infrastructure.Security;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace EstrelaClick.Tests.IntegrationTests.Endpoints;

public sealed class PlacasFabricaEndpointsTests : IAsyncLifetime
{
    private const string ApiKeyValida = "chave-fabrica-teste-integracao-123";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_password123!")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", _container.GetConnectionString());
            builder.UseSetting("FactorySecurity:ApiKey", ApiKeyValida);
            builder.UseSetting("App:BaseRedirectUrl", "https://redirect.estrelaclick.com.br");
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task EndpointsFabrica_DevemRetornar401Unauthorized_QuandoNaoAutenticados()
    {
        // Act
        var postGerarLote = await _client.PostAsJsonAsync("/api/placas/gerar-lote", new GerarLotePlacasCommand("LOTE-SEM-AUTH", 5));
        var getProxima = await _client.GetAsync("/api/placas/proxima-pendente");
        var postConfirmar = await _client.PostAsJsonAsync("/api/placas/confirmar-gravacao", new ConfirmarGravacaoPlacaCommand(Guid.NewGuid().ToString("N"), "04AABB"));

        // Assert
        postGerarLote.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        getProxima.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postConfirmar.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FluxoCompletoFabrica_GerarLote_ConsumirFila_ConfirmarGravacao()
    {
        // 1. Configurar cliente autenticado com X-Api-Key
        _client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HeaderName, ApiKeyValida);

        var numeroLote = $"LOTE-INT-{Guid.NewGuid():N}"[..16];
        var quantidade = 3;

        // 2. Gerar lote de 3 placas
        var gerarLoteResponse = await _client.PostAsJsonAsync("/api/placas/gerar-lote", new GerarLotePlacasCommand(numeroLote, quantidade));
        gerarLoteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loteCriado = await gerarLoteResponse.Content.ReadFromJsonAsync<LotePlacasCriadoDto>();
        loteCriado.Should().NotBeNull();
        loteCriado!.NumeroLote.Should().Be(numeroLote);
        loteCriado.QuantidadeCriada.Should().Be(3);
        loteCriado.Slugs.Should().HaveCount(3);

        // 3. Tentar gerar novamente o mesmo lote -> deve retornar 409 Conflict
        var loteDuplicadoResponse = await _client.PostAsJsonAsync("/api/placas/gerar-lote", new GerarLotePlacasCommand(numeroLote, 2));
        loteDuplicadoResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 4. Consumir sequencialmente as 3 placas na fila
        for (int i = 0; i < quantidade; i++)
        {
            var proximaResponse = await _client.GetAsync($"/api/placas/proxima-pendente?numeroLote={numeroLote}");
            proximaResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var placaPendente = await proximaResponse.Content.ReadFromJsonAsync<PlacaPendenteDto>();
            placaPendente.Should().NotBeNull();
            placaPendente!.NumeroLote.Should().Be(numeroLote);
            placaPendente.UrlRedirecionamento.Should().Be($"https://redirect.estrelaclick.com.br/r/{placaPendente.Slug}");
            placaPendente.DataReservaUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

            // Confirmar gravação
            var chipUid = $"04AABB{i:X4}";
            var confirmarResponse = await _client.PostAsJsonAsync("/api/placas/confirmar-gravacao",
                new ConfirmarGravacaoPlacaCommand(placaPendente.Slug, chipUid));

            confirmarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var confirmada = await confirmarResponse.Content.ReadFromJsonAsync<PlacaGravadaDto>();
            confirmada.Should().NotBeNull();
            confirmada!.Slug.Should().Be(placaPendente.Slug);
            confirmada.ChipUid.Should().Be(chipUid);
        }

        // 5. Após consumir todas as placas do lote, a fila deve retornar 404 Fila Vazia
        var filaVaziaResponse = await _client.GetAsync($"/api/placas/proxima-pendente?numeroLote={numeroLote}");
        filaVaziaResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 6. Validar no banco de dados que todas as 3 placas estão com status Gravada
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placasBanco = await db.Placas
                .Where(p => p.NumeroLote == numeroLote)
                .ToListAsync();

            placasBanco.Should().HaveCount(3);
            placasBanco.Should().OnlyContain(p => p.StatusPlaca == StatusPlaca.Gravada);
            placasBanco.Should().OnlyContain(p => !string.IsNullOrEmpty(p.ChipUid));
        }
    }

    [Fact]
    public async Task OperadorLogin_QuandoCredenciaisValidas_DeveRetornarTokenJWT()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/operador-login",
            new AutenticarOperadorCommand("operador", "operador123"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResult = await response.Content.ReadFromJsonAsync<AuthTokenResult>();
        authResult.Should().NotBeNull();
        authResult!.Token.Should().NotBeNullOrWhiteSpace();
        authResult.OperadorId.Should().Be("OP-BANCADA-01");
        authResult.Nome.Should().Be("Operador de Bancada 01");
    }

    [Fact]
    public async Task ObterResumoLote_QuandoLoteExistir_DeveRetornarMetricasCorretas()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove(ApiKeyAuthenticationOptions.HeaderName);
        _client.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HeaderName, ApiKeyValida);

        var lote = "LOTE-RESUMO-TESTE";
        await _client.PostAsJsonAsync("/api/placas/gerar-lote", new GerarLotePlacasCommand(lote, 4));

        // Act
        var response = await _client.GetAsync($"/api/placas/lotes/{lote}/resumo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resumo = await response.Content.ReadFromJsonAsync<LoteResumoDto>();
        resumo.Should().NotBeNull();
        resumo!.NumeroLote.Should().Be(lote);
        resumo.TotalPlacas.Should().Be(4);
        resumo.TotalPendentes.Should().Be(4);
        resumo.TotalGravadas.Should().Be(0);
    }
}
