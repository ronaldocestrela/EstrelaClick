using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class ObterResumoLoteUseCaseTests
{
    private readonly IPlacaRepository _placaRepositoryMock = Substitute.For<IPlacaRepository>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecutarAsync_ComNumeroLoteVazio_DeveRetornarFalhaLoteInvalido(string numeroLote)
    {
        // Arrange
        var useCase = new ObterResumoLoteUseCase(_placaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync(numeroLote);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.LoteInvalido);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLoteNaoExiste_DeveRetornarFalhaLoteNaoEncontrado()
    {
        // Arrange
        _placaRepositoryMock.ObterPorLoteAsync("LOTE-INEXISTENTE", Arg.Any<CancellationToken>())
            .Returns(new List<Placa>());

        var useCase = new ObterResumoLoteUseCase(_placaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync("LOTE-INEXISTENTE");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.LoteNaoEncontrado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLoteExiste_DeveCalcularTotaisComSucesso()
    {
        // Arrange
        var lote = "LOTE-2026-01";
        var placas = new List<Placa>
        {
            Placa.Criar(Guid.NewGuid().ToString("N"), lote).Value!, // Criada
            Placa.Criar(Guid.NewGuid().ToString("N"), lote).Value!, // Criada
            Placa.Criar(Guid.NewGuid().ToString("N"), lote).Value!, // Gravada
            Placa.Criar(Guid.NewGuid().ToString("N"), lote).Value!  // Vinculada
        };

        // Simula transições
        placas[2].RegistrarGravacao("04:A1:B2:C3:D4:E5:F6");
        placas[3].RegistrarGravacao("04:F6:E5:D4:C3:B2:A1");
        placas[3].VincularEmpresa(Guid.NewGuid(), "https://google.com");

        _placaRepositoryMock.ObterPorLoteAsync(lote, Arg.Any<CancellationToken>())
            .Returns(placas);

        var useCase = new ObterResumoLoteUseCase(_placaRepositoryMock);

        // Act
        var result = await useCase.ExecutarAsync(lote);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NumeroLote.Should().Be(lote);
        result.Value.TotalPlacas.Should().Be(4);
        result.Value.TotalPendentes.Should().Be(2);
        result.Value.TotalGravadas.Should().Be(1);
        result.Value.TotalVinculadas.Should().Be(1);
    }
}
