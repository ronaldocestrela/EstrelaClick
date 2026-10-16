using EstrelaClick.Backend.Domain.Entities;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class AcessoMetricaTests
{
    [Fact]
    public void Criar_DeveRetornarSucesso_QuandoDadosForemValidos()
    {
        var placaId = Guid.NewGuid();

        var result = AcessoMetrica.Criar(
            placaId,
            userAgent: "Mozilla/5.0",
            ipHash: "hash123",
            origemReferer: "https://google.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PlacaId.Should().Be(placaId);
        result.Value.UserAgent.Should().Be("Mozilla/5.0");
        result.Value.IpHash.Should().Be("hash123");
        result.Value.OrigemReferer.Should().Be("https://google.com");
        result.Value.DataHoraUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Criar_DeveFalhar_QuandoPlacaIdForInvalido()
    {
        var result = AcessoMetrica.Criar(
            Guid.Empty,
            userAgent: "Mozilla/5.0",
            ipHash: "hash123",
            origemReferer: "https://google.com");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("acessoMetrica.placaId.invalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_DeveFalhar_QuandoIpHashForInvalido(string ipHash)
    {
        var result = AcessoMetrica.Criar(
            Guid.NewGuid(),
            userAgent: "Mozilla/5.0",
            ipHash: ipHash,
            origemReferer: "https://google.com");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("acessoMetrica.ipHash.invalido");
    }

    [Fact]
    public void Criar_DevePermitirCamposOpcionaisNulos()
    {
        var result = AcessoMetrica.Criar(Guid.NewGuid(), null, "hash123", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserAgent.Should().BeNull();
        result.Value.OrigemReferer.Should().BeNull();
    }
}
