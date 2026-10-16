using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class PlacaVinculacaoTests
{
    [Fact]
    public void VincularEmpresa_DeveAtualizarEstado_QuandoPlacaEstiverGravada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");
        var empresaId = Guid.NewGuid();

        var result = placa.VincularEmpresa(empresaId, "https://empresa.exemplo.com/avaliacao");

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.Vinculada);
        placa.EmpresaId.Should().Be(empresaId);
        placa.UrlDestino.Should().Be("https://empresa.exemplo.com/avaliacao");
        placa.DataAtivacaoUtc.Should().NotBeNull();
        placa.DataAtivacaoUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void VincularEmpresa_DeveFalhar_QuandoStatusNaoForGravada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;

        var result = placa.VincularEmpresa(Guid.NewGuid(), "https://empresa.exemplo.com/avaliacao");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.status.transicao.invalida");
    }

    [Fact]
    public void VincularEmpresa_DeveFalhar_QuandoEmpresaIdForInvalido()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var result = placa.VincularEmpresa(Guid.Empty, "https://empresa.exemplo.com/avaliacao");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.empresaId.invalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("empresa.exemplo.com")]
    [InlineData("ftp://empresa.exemplo.com")]
    public void VincularEmpresa_DeveFalhar_QuandoUrlDestinoForInvalida(string urlDestino)
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var result = placa.VincularEmpresa(Guid.NewGuid(), urlDestino);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.urlDestino.invalida");
    }
}
