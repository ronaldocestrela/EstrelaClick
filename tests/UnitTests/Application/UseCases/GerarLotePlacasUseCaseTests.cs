using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class GerarLotePlacasUseCaseTests
{
    private readonly IPlacaRepository _placaRepository;
    private readonly ISlugGenerator _slugGenerator;
    private readonly GerarLotePlacasUseCase _useCase;

    public GerarLotePlacasUseCaseTests()
    {
        _placaRepository = Substitute.For<IPlacaRepository>();
        _slugGenerator = Substitute.For<ISlugGenerator>();
        _useCase = new GerarLotePlacasUseCase(_placaRepository, _slugGenerator);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoNumeroLoteForVazio()
    {
        var command = new GerarLotePlacasCommand("", 10);

        var result = await _useCase.ExecutarAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.LoteInvalido);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoQuantidadeForInvalida()
    {
        var commandMenor = new GerarLotePlacasCommand("LOTE-01", 0);
        var commandMaior = new GerarLotePlacasCommand("LOTE-01", 10001);

        var resultMenor = await _useCase.ExecutarAsync(commandMenor);
        var resultMaior = await _useCase.ExecutarAsync(commandMaior);

        resultMenor.IsFailure.Should().BeTrue();
        resultMenor.Error.Should().Be(PlacaErrors.QuantidadeInvalida);
        resultMaior.IsFailure.Should().BeTrue();
        resultMaior.Error.Should().Be(PlacaErrors.QuantidadeInvalida);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoLoteJaExistir()
    {
        var command = new GerarLotePlacasCommand("LOTE-DUPLICADO", 5);
        _placaRepository.ExisteLoteAsync("LOTE-DUPLICADO", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _useCase.ExecutarAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.LoteJaExiste);
        await _placaRepository.DidNotReceive().InserirLoteAsync(Arg.Any<IEnumerable<Placa>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecutarAsync_DeveGerarPlacasEInserirLote_QuandoDadosForemValidos()
    {
        var command = new GerarLotePlacasCommand("LOTE-NOVO", 3);
        _placaRepository.ExisteLoteAsync("LOTE-NOVO", Arg.Any<CancellationToken>())
            .Returns(false);

        _slugGenerator.GerarSlug().Returns(
            _ => Guid.NewGuid().ToString("N"),
            _ => Guid.NewGuid().ToString("N"),
            _ => Guid.NewGuid().ToString("N"));

        var result = await _useCase.ExecutarAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NumeroLote.Should().Be("LOTE-NOVO");
        result.Value.QuantidadeCriada.Should().Be(3);
        result.Value.Slugs.Should().HaveCount(3);

        await _placaRepository.Received(1).InserirLoteAsync(
            Arg.Is<IEnumerable<Placa>>(placas => placas.Count() == 3 && placas.All(p => p.NumeroLote == "LOTE-NOVO")),
            Arg.Any<CancellationToken>());
    }
}
