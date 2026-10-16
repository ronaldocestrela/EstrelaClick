using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;

namespace EstrelaClick.Tests.UnitTests.Domain.Entities;

public class PlacaReservaTests
{
    [Fact]
    public void ReservarParaGravacao_DeveAtualizarEstado_QuandoPlacaEstiverCriada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        var momentoUtc = DateTime.UtcNow;

        var result = placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", momentoUtc);

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.EmGravacao);
        placa.DataReservaUtc.Should().Be(momentoUtc);
        placa.OperadorReservaId.Should().Be("operador-01");
    }

    [Fact]
    public void ReservarParaGravacao_DeveFalhar_QuandoPlacaEstiverReservadaENaoExpirada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        var reservaInicial = DateTime.UtcNow;
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", reservaInicial);

        var tentativaMomento = reservaInicial.AddSeconds(30);
        var result = placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-02", tentativaMomento);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.reserva.ativa");
        placa.OperadorReservaId.Should().Be("operador-01");
    }

    [Fact]
    public void ReservarParaGravacao_DevePermitirNovaReserva_QuandoReservaAnteriorExpirou()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        var reservaInicial = DateTime.UtcNow.AddMinutes(-5);
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", reservaInicial);

        var novoMomento = DateTime.UtcNow;
        var result = placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-02", novoMomento);

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.EmGravacao);
        placa.OperadorReservaId.Should().Be("operador-02");
        placa.DataReservaUtc.Should().Be(novoMomento);
    }

    [Fact]
    public void ReservarParaGravacao_DeveFalhar_QuandoPlacaJaEstiverGravadaOuVinculada()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        placa.RegistrarGravacao("04AABBCCDD");

        var result = placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("placa.status.transicao.invalida");
    }

    [Fact]
    public void RegistrarGravacao_DeveFuncionar_QuandoPlacaEstiverEmGravacao()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", DateTime.UtcNow);

        var result = placa.RegistrarGravacao("04AABBCCDD");

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.Gravada);
        placa.ChipUid.Should().Be("04AABBCCDD");
        placa.DataGravacaoUtc.Should().NotBeNull();
        placa.DataReservaUtc.Should().BeNull();
        placa.OperadorReservaId.Should().BeNull();
    }

    [Fact]
    public void LiberarReserva_DeveRetornarParaCriada_QuandoEstiverEmGravacao()
    {
        var placa = Placa.Criar(Guid.NewGuid().ToString("N"), "LOTE-01").Value!;
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", DateTime.UtcNow);

        var result = placa.LiberarReserva();

        result.IsSuccess.Should().BeTrue();
        placa.StatusPlaca.Should().Be(StatusPlaca.Criada);
        placa.DataReservaUtc.Should().BeNull();
        placa.OperadorReservaId.Should().BeNull();
    }
}
