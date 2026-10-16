using EstrelaClick.Shared.SharedKernel.Nfc;
using FluentAssertions;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Mobile;

public class NdefUriHelperTests
{
    [Theory]
    [InlineData("https://estrelaclick.com/r/abc12345", 0x04, "estrelaclick.com/r/abc12345")]
    [InlineData("http://estrelaclick.com/r/xyz987", 0x03, "estrelaclick.com/r/xyz987")]
    [InlineData("https://meudominio.com.br/r/lote-01-slug-99", 0x04, "meudominio.com.br/r/lote-01-slug-99")]
    public void EncodeUri_WithValidHttpOrHttpsUrl_ShouldReturnSuccessWithCorrectPrefixAndPayload(
        string inputUrl, byte expectedPrefixCode, string expectedPayload)
    {
        // Act
        var result = NdefUriHelper.EncodeUri(inputUrl);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrefixCode.Should().Be(expectedPrefixCode);
        result.Value.SubPath.Should().Be(expectedPayload);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://servidor.com")]
    [InlineData("texto-invalido-sem-protocolo")]
    [InlineData("javascript:alert(1)")]
    public void EncodeUri_WithInvalidUrl_ShouldReturnFailure(string invalidUrl)
    {
        // Act
        var result = NdefUriHelper.EncodeUri(invalidUrl);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NfcErrors.InvalidUri.Code);
    }

    [Fact]
    public void FormatUid_WithValidBytes_ShouldReturnFormattedUpperCaseHex()
    {
        // Arrange
        byte[] uid = [0x04, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E, 0x6F];

        // Act
        var result = NdefUriHelper.FormatUid(uid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("04:1A:2B:3C:4D:5E:6F");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new byte[0])]
    public void FormatUid_WithNullOrEmpty_ShouldReturnFailure(byte[]? uid)
    {
        // Act
        var result = NdefUriHelper.FormatUid(uid);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NfcErrors.InvalidUid.Code);
    }

    [Fact]
    public void CalculateRequiredBytes_WithStandardHttpsUri_ShouldCalculateCorrectNdefFrameSize()
    {
        // Arrange
        // Exemplo: prefix 0x04 (1 byte) + "estrelaclick.com/r/slug" (23 bytes) = 24 bytes de payload
        // Mais header NDEF record padrão (3 a 5 bytes dependendo do tamanho)
        string uri = "https://estrelaclick.com/r/12345678901234567890123456789012";

        // Act
        var result = NdefUriHelper.CalculateRequiredBytes(uri);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // NTAG213 tem capacidade utilizável de 144 bytes; a URL gerada deve caber com folga
        result.Value.Should().BeLessThan(144);
    }
}
