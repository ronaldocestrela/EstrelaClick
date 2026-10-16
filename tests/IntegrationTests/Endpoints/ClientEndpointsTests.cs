using System.Net;
using System.Net.Http.Json;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Backend.Infrastructure.Persistence;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace EstrelaClick.Tests.IntegrationTests.Endpoints;

public sealed class ClientEndpointsTests : IAsyncLifetime
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
    public async Task VerificarOnboarding_QuandoPlacaEstiverGravada_DeveRetornarDisponivel()
    {
        var slug = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("04AABBCCDD");
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/client/onboarding/verificar/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        json.GetProperty("slug").GetString().Should().Be(slug);
        json.GetProperty("disponivelParaAtivacao").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AtivarOnboarding_DeveCriarEmpresaEVincularPlaca()
    {
        var slug = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("04AABBCCDD");
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        var request = new AtivarPlacaOnboardingRequest(
            Slug: slug,
            UrlDestino: "https://g.page/r/minha-loja/review",
            RazaoSocial: "Lojinha do Bairro",
            Email: "lojinha@bairro.com",
            Telefone: "11987654321");

        var response = await _client.PostAsJsonAsync("/api/client/onboarding/ativar", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PlacaAtivadaDto>();
        result.Should().NotBeNull();
        result!.Slug.Should().Be(slug);
        result.UrlDestino.Should().Be("https://g.page/r/minha-loja/review");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placaDb = await db.Placas.FirstAsync(p => p.Slug == slug);
            placaDb.StatusPlaca.Should().Be(StatusPlaca.Vinculada);
            placaDb.EmpresaId.Should().NotBeNull();
            placaDb.EmpresaId.Should().Be(result.EmpresaId);
        }
    }

    [Fact]
    public async Task AtualizarUrlPlaca_DeveAtualizarNoBancoComSucesso()
    {
        var slug = Guid.NewGuid().ToString("N");
        Guid empresaId;
        Guid placaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var empresa = Empresa.Criar("Empresa Teste", "teste@empresa.com", "11999999999").Value!;
            await db.Empresas.AddAsync(empresa);

            var placa = Placa.Criar(slug).Value!;
            placa.RegistrarGravacao("04AABBCCDD");
            placa.VincularEmpresa(empresa.Id, "https://url-antiga.com");
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();

            empresaId = empresa.Id;
            placaId = placa.Id;
        }

        var updateRequest = new AtualizarUrlPlacaRequest(placaId, "https://nova-url-atualizada.com");
        var response = await _client.PutAsJsonAsync($"/api/client/empresas/{empresaId}/placas/{placaId}/url", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placaDb = await db.Placas.FindAsync(placaId);
            placaDb!.UrlDestino.Should().Be("https://nova-url-atualizada.com/");
        }
    }

    [Fact]
    public async Task ObterAnalytics_DeveRetornarMetricasAgregadas()
    {
        Guid empresaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var empresa = Empresa.Criar("Empresa Analytics", "analytics@empresa.com", "11999999999").Value!;
            await db.Empresas.AddAsync(empresa);

            var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
            placa.RegistrarGravacao("04AABBCCDD");
            placa.VincularEmpresa(empresa.Id, "https://site.com");
            await db.Placas.AddAsync(placa);

            var metrica = AcessoMetrica.Criar(placa.Id, "Mozilla/5.0 (Linux; Android 14)", "hash123", "https://instagram.com").Value!;
            await db.AcessosMetricas.AddAsync(metrica);

            await db.SaveChangesAsync();
            empresaId = empresa.Id;
        }

        var response = await _client.GetAsync($"/api/client/empresas/{empresaId}/analytics?dias=30");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var analytics = await response.Content.ReadFromJsonAsync<AnalyticsClienteDto>();
        analytics.Should().NotBeNull();
        analytics!.TotalToquesPeriodo.Should().Be(1);
        analytics.Dispositivos.Should().ContainSingle(d => d.Categoria == "Android");
    }
}
