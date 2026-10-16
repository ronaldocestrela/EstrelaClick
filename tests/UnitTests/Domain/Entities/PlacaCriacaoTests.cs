using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class PlacaCriacaoTests
{
    [Fact]
    public void StatusPlaca_DeveConterEstadosEsperados()
    {
        Enum.IsDefined(StatusPlaca.Criada).Should().BeTrue();
        Enum.IsDefined(StatusPlaca.Gravada).Should().BeTrue();
        Enum.IsDefined(StatusPlaca.Vinculada).Should().BeTrue();
    }

    [Fact]
    public void Criar_DeveRetornarSucesso_QuandoSlugForGuidSemHifens()
    {
        var slug = Guid.NewGuid().ToString("N");

        var result = Placa.Criar(slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Slug.Should().Be(slug);
        result.Value.StatusPlaca.Should().Be(StatusPlaca.Criada);
        result.Value.UrlDestino.Should().BeNull();
        result.Value.ChipUid.Should().BeNull();
        result.Value.EmpresaId.Should().BeNull();
        result.Value.DataCriacaoUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.Value.DataGravacaoUtc.Should().BeNull();
        result.Value.DataAtivacaoUtc.Should().BeNull();
    }

    [Fact]
    public void Criar_DeveRetornarSucesso_ComNumeroLoteInformado()
    {
        var slug = Guid.NewGuid().ToString("N");
        var numeroLote = "LOTE-2026-01";

        var result = Placa.Criar(slug, numeroLote);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NumeroLote.Should().Be(numeroLote);
    }

    [Fact]
    public void Criar_DeveAtribuirLotePadrao_QuandoNumeroLoteNaoForEspecificado()
    {
        var slug = Guid.NewGuid().ToString("N");

        var result = Placa.Criar(slug);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NumeroLote.Should().Be("PADRAO");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_DeveRetornarFalha_QuandoNumeroLoteForVazio(string loteInvalido)
    {
        var slug = Guid.NewGuid().ToString("N");

        var result = Placa.Criar(slug, loteInvalido);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.numeroLote.invalido");
    }

    [Fact]
    public void Criar_DeveRetornarFalha_QuandoNumeroLoteExcederLimite()
    {
        var slug = Guid.NewGuid().ToString("N");
        var loteLongo = new string('A', 33);

        var result = Placa.Criar(slug, loteLongo);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.numeroLote.invalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("slug-invalido")]
    [InlineData("4f4f6220-0747-4cd4-97d2-d5df7c4338f3")]
    public void Criar_DeveRetornarFalha_QuandoSlugNaoForGuidSemHifens(string slug)
    {
        var result = Placa.Criar(slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.slug.invalido");
    }
}
