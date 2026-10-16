using System.Net;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Backend.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace EstrelaClick.Tests.IntegrationTests.Endpoints;

public sealed class RedirecionamentoEndpointTests : IAsyncLifetime
{
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
        });

        // Configura client sem auto-redirect para inspecionar o status HTTP 302 e header Location
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

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
    public async Task GetRedirecionamento_QuandoPlacaVinculada_DeveRetornar302EEnfileirarMetrica()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");
        const string urlDestino = "https://google.com/search?q=estrelaclick";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var empresa = Empresa.Criar("Empresa Teste", "teste@empresa.com", "11999999999", "identity-user-123").Value!;
            await db.Empresas.AddAsync(empresa);

            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("CHIP-UID-001");
            placa.VincularEmpresa(empresa.Id, urlDestino);

            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/r/{slug}");
        request.Headers.Add("User-Agent", "IntegrationTest-Agent");
        request.Headers.Add("Referer", "https://nfc.local");

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be(urlDestino);
        response.Headers.Contains("Server-Timing").Should().BeTrue();

        // Aguarda o flush periódico do BackgroundService (máx 3s)
        await Task.Delay(2500);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var metrica = await db.AcessosMetricas
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserAgent == "IntegrationTest-Agent");

            metrica.Should().NotBeNull();
            metrica!.OrigemReferer.Should().StartWith("https://nfc.local");
            metrica.IpHash.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetRedirecionamento_QuandoPlacaGravadaSemVinculo_DeveRedirecionarParaOnboarding()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("CHIP-UID-002");
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/r/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/ativar/{slug}");
    }

    [Fact]
    public async Task GetRedirecionamento_QuandoFiltroSatisfacaoAtivo_DeveRedirecionarParaFeedback()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var empresa = Empresa.Criar("Empresa Teste", "teste@empresa.com", "11999999999").Value!;
            await db.Empresas.AddAsync(empresa);

            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("CHIP-UID-003");
            placa.VincularEmpresa(empresa.Id, "https://google.com/review");
            placa.ConfigurarFiltroSatisfacao(true, "https://wa.me/5511999999999");
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/r/{slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/feedback/{slug}");
    }

    [Fact]
    public async Task GetRedirecionamento_QuandoSlugNaoExiste_DeveRetornar404()
    {
        // Arrange
        var slugInexistente = Guid.NewGuid().ToString("N");

        // Act
        var response = await _client.GetAsync($"/r/{slugInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
