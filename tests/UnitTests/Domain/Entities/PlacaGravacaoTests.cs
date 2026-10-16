using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class PlacaGravacaoTests
{
    [Fact]
    public void RegistrarGravacao_DeveAtualizarEstado_QuandoPlacaEstiverCriada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;

        var result = placa.RegistrarGravacao("04AABBCCDD");

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.Gravada);
        placa.ChipUid.Should().Be("04AABBCCDD");
        placa.DataGravacaoUtc.Should().NotBeNull();
        placa.DataGravacaoUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegistrarGravacao_DeveFalhar_QuandoChipUidForInvalido(string chipUid)
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;

        var result = placa.RegistrarGravacao(chipUid);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.chipUid.invalido");
        placa.StatusPlaca.Should().Be(StatusPlaca.Criada);
    }

    [Fact]
    public void RegistrarGravacao_DeveFalhar_QuandoStatusNaoForCriada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N")).Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var result = placa.RegistrarGravacao("1122334455");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.status.transicao.invalida");
    }
}
