using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class PlacaClientConfigTests
{
    private static Placa CriarPlacaVinculada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");
        placa.VincularEmpresa(Guid.NewGuid(), "https://empresa.exemplo.com/origem");
        return placa;
    }

    [Fact]
    public void AtualizarUrlDestino_DeveAtualizarUrlComSucesso_QuandoPlacaEstiverVinculadaEUrlValida()
    {
        var placa = CriarPlacaVinculada();
        var novaUrl = "https://g.page/r/exemplo-loja/review";

        var result = placa.AtualizarUrlDestino(novaUrl);

        result.IsSuccess.Should().BeTrue();
        placa.UrlDestino.Should().Be("https://g.page/r/exemplo-loja/review");
    }

    [Fact]
    public void AtualizarUrlDestino_DeveFalhar_QuandoPlacaNaoEstiverVinculada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var result = placa.AtualizarUrlDestino("https://g.page/r/exemplo-loja/review");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.status.transicao.invalida");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("g.page/review")]
    [InlineData("ftp://g.page/review")]
    [InlineData("javascript:alert(1)")]
    public void AtualizarUrlDestino_DeveFalhar_QuandoUrlForInvalida(string urlInvalida)
    {
        var placa = CriarPlacaVinculada();

        var result = placa.AtualizarUrlDestino(urlInvalida);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.urlDestino.invalida");
    }

    [Fact]
    public void ConfigurarFiltroSatisfacao_DeveAtivarFiltro_QuandoDadosForemValidosEPlacaVinculada()
    {
        var placa = CriarPlacaVinculada();
        var urlSac = "https://wa.me/5511999999999?text=Ouvidoria";

        var result = placa.ConfigurarFiltroSatisfacao(ativo: true, urlSacInterno: urlSac);

        result.IsSuccess.Should().BeTrue();
        placa.FiltroSatisfacaoAtivo.Should().BeTrue();
        placa.UrlSacInterno.Should().Be("https://wa.me/5511999999999?text=Ouvidoria");
    }

    [Fact]
    public void ConfigurarFiltroSatisfacao_DeveDesativarFiltro_ELimparUrlSac()
    {
        var placa = CriarPlacaVinculada();
        placa.ConfigurarFiltroSatisfacao(ativo: true, urlSacInterno: "https://wa.me/5511999999999");

        var result = placa.ConfigurarFiltroSatisfacao(ativo: false, urlSacInterno: null);

        result.IsSuccess.Should().BeTrue();
        placa.FiltroSatisfacaoAtivo.Should().BeFalse();
        placa.UrlSacInterno.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("wa.me/sac")]
    public void ConfigurarFiltroSatisfacao_DeveFalhar_QuandoAtivoEUrlSacInvalida(string urlInvalida)
    {
        var placa = CriarPlacaVinculada();

        var result = placa.ConfigurarFiltroSatisfacao(ativo: true, urlSacInterno: urlInvalida);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.filtroSatisfacao.urlSacInvalida");
    }

    [Fact]
    public void ConfigurarFiltroSatisfacao_DeveFalhar_QuandoPlacaNaoEstiverVinculada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;

        var result = placa.ConfigurarFiltroSatisfacao(ativo: true, urlSacInterno: "https://wa.me/5511999999999");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.status.transicao.invalida");
    }
}
