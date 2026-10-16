using System.IO.Compression;
using System.Text;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Infrastructure.QrCode;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.QrCode;

public sealed class ZipLoteArchiveServiceTests
{
    private readonly ZipLoteArchiveService _service = new();

    [Fact]
    public void CriarPacoteLote_ComItensValidos_DeveGerarZipComPastasPngSvgEManifestCsv()
    {
        // Arrange
        var numeroLote = "LOTE-2026-TESTE";
        var slug1 = Guid.NewGuid().ToString("N");
        var slug2 = Guid.NewGuid().ToString("N");

        var pngBytes = new byte[] { 137, 80, 78, 71 };
        var svgBytes = "<svg></svg>"u8.ToArray();

        var itens = new List<(string Slug, string UrlRedirecionamento, byte[] PngConteudo, byte[] SvgConteudo)>
        {
            (slug1, $"https://estrela.click/r/{slug1}", pngBytes, svgBytes),
            (slug2, $"https://estrela.click/r/{slug2}", pngBytes, svgBytes)
        };

        // Act
        var result = _service.CriarPacoteLote(numeroLote, itens);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        using var memoryStream = new MemoryStream(result.Value!);
        using var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        var entryNames = zip.Entries.Select(e => e.FullName).ToList();

        entryNames.Should().Contain($"png/{slug1}.png");
        entryNames.Should().Contain($"png/{slug2}.png");
        entryNames.Should().Contain($"svg/{slug1}.svg");
        entryNames.Should().Contain($"svg/{slug2}.svg");
        entryNames.Should().Contain("manifest.csv");

        var manifestEntry = zip.GetEntry("manifest.csv");
        manifestEntry.Should().NotBeNull();

        using var reader = new StreamReader(manifestEntry!.Open(), Encoding.UTF8);
        var csvContent = reader.ReadToEnd();

        csvContent.Should().Contain("Slug;UrlRedirecionamento;NumeroLote");
        csvContent.Should().Contain($"{slug1};https://estrela.click/r/{slug1};{numeroLote}");
        csvContent.Should().Contain($"{slug2};https://estrela.click/r/{slug2};{numeroLote}");
    }

    [Fact]
    public void CriarPacoteLote_ComItensVazios_DeveRetornarFalhaLoteVazio()
    {
        var result = _service.CriarPacoteLote("LOTE-VAZIO", new List<(string, string, byte[], byte[])>());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.LoteVazio);
    }
}
