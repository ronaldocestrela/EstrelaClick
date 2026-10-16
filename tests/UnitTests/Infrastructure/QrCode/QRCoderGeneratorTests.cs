using System.Text;
using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Infrastructure.QrCode;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Infrastructure.QrCode;

public sealed class QRCoderGeneratorTests
{
    private readonly QRCoderGenerator _generator = new();

    [Fact]
    public void GerarPng300Dpi_ComPayloadValido_DeveRetornarPngComChunkPhys300Dpi()
    {
        // Arrange
        const string payload = "https://estrela.click/r/0123456789abcdef0123456789abcdef";

        // Act
        var result = _generator.GerarPng300Dpi(payload, pixelsPorModulo: 15);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var bytes = result.Value!;

        // Valida assinatura padrão PNG: 137, 80, 78, 71, 13, 10, 26, 10
        bytes.Take(8).Should().BeEquivalentTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, options => options.WithStrictOrdering());

        // Valida presença do chunk pHYs (Physical pixel dimensions)
        var physChunkHeader = Encoding.ASCII.GetBytes("pHYs");
        var chunkIndex = LocalizarSequencia(bytes, physChunkHeader);
        chunkIndex.Should().BeGreaterThan(0, "o chunk pHYs deve estar presente no PNG para birôs de impressão");

        // 300 DPI = 11811 pixels/metro = 0x00, 0x00, 0x2E, 0x23 (em big-endian)
        var valorEsperadoPpm = new byte[] { 0x00, 0x00, 0x2E, 0x23 };
        var ppuX = bytes.Skip(chunkIndex + 4).Take(4).ToArray();
        var ppuY = bytes.Skip(chunkIndex + 8).Take(4).ToArray();
        var unitSpecifier = bytes[chunkIndex + 12]; // 1 = metro

        ppuX.Should().BeEquivalentTo(valorEsperadoPpm, options => options.WithStrictOrdering());
        ppuY.Should().BeEquivalentTo(valorEsperadoPpm, options => options.WithStrictOrdering());
        unitSpecifier.Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GerarPng300Dpi_ComPayloadVazio_DeveRetornarFalhaConteudoInvalido(string payloadInvalido)
    {
        var result = _generator.GerarPng300Dpi(payloadInvalido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.ConteudoInvalido);
    }

    [Fact]
    public void GerarSvg_ComPayloadValido_DeveRetornarSvgValido()
    {
        // Arrange
        const string payload = "https://estrela.click/r/0123456789abcdef0123456789abcdef";

        // Act
        var result = _generator.GerarSvg(payload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var svgTexto = Encoding.UTF8.GetString(result.Value!);
        svgTexto.Should().Contain("<svg");
        svgTexto.Should().Contain("</svg>");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GerarSvg_ComPayloadVazio_DeveRetornarFalhaConteudoInvalido(string payloadInvalido)
    {
        var result = _generator.GerarSvg(payloadInvalido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.ConteudoInvalido);
    }

    private static int LocalizarSequencia(byte[] origem, byte[] sequencia)
    {
        for (int i = 0; i <= origem.Length - sequencia.Length; i++)
        {
            var match = true;
            for (int j = 0; j < sequencia.Length; j++)
            {
                if (origem[i + j] != sequencia[j])
                {
                    match = false;
                    break;
                }
            }

            if (match) return i;
        }

        return -1;
    }
}
