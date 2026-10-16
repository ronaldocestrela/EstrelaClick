using System.IO.Compression;
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

public sealed class QrCodeEndpointsTests : IAsyncLifetime
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
    public async Task GetQrCodePng_QuandoPlacaExiste_DeveRetornar200ComImagemPng()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa = Placa.Criar(slug, "LOTE-TESTE-PNG").Value!;
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/placas/{slug}/qrcode.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/png");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
        bytes.Take(4).Should().BeEquivalentTo(new byte[] { 137, 80, 78, 71 });
    }

    [Fact]
    public async Task GetQrCodeSvg_QuandoPlacaExiste_DeveRetornar200ComSvg()
    {
        // Arrange
        var slug = Guid.NewGuid().ToString("N");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa = Placa.Criar(slug, "LOTE-TESTE-SVG").Value!;
            await db.Placas.AddAsync(placa);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/placas/{slug}/qrcode.svg");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/svg+xml");

        var svgContent = await response.Content.ReadAsStringAsync();
        svgContent.Should().Contain("<svg");
        svgContent.Should().Contain("</svg>");
    }

    [Fact]
    public async Task ExportarLoteZip_QuandoLoteExiste_DeveRetornar200ComZipEManifesto()
    {
        // Arrange
        var lote = "LOTE-EXP-01";
        var slug1 = Guid.NewGuid().ToString("N");
        var slug2 = Guid.NewGuid().ToString("N");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EstrelaClickDbContext>();
            var placa1 = Placa.Criar(slug1, lote).Value!;
            var placa2 = Placa.Criar(slug2, lote).Value!;
            await db.Placas.AddRangeAsync(placa1, placa2);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/placas/lotes/{lote}/exportar-zip");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        using var memoryStream = new MemoryStream(zipBytes);
        using var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        var entries = zip.Entries.Select(e => e.FullName).ToList();
        entries.Should().Contain($"png/{slug1}.png");
        entries.Should().Contain($"png/{slug2}.png");
        entries.Should().Contain($"svg/{slug1}.svg");
        entries.Should().Contain($"svg/{slug2}.svg");
        entries.Should().Contain("manifest.csv");
    }

    [Fact]
    public async Task ExportarLoteZip_QuandoLoteInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/placas/lotes/LOTE-FANTASMA/exportar-zip");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
