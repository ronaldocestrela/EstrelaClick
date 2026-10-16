using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using NSubstitute;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public class ConfirmarGravacaoPlacaUseCaseTests
{
    private readonly IPlacaRepository _placaRepository;
    private readonly ConfirmarGravacaoPlacaUseCase _useCase;

    public ConfirmarGravacaoPlacaUseCaseTests()
    {
        _placaRepository = Substitute.For<IPlacaRepository>();
        _useCase = new ConfirmarGravacaoPlacaUseCase(_placaRepository);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoSlugForInvalido()
    {
        var command = new ConfirmarGravacaoPlacaCommand("slug-invalido-com-traco", "04AABBCCDD");

        var result = await _useCase.ExecutarAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.SlugInvalido);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoChipUidForInvalido(string? chipUid)
    {
        var command = new ConfirmarGravacaoPlacaCommand(Guid.NewGuid().ToString("N"), chipUid!);

        var result = await _useCase.ExecutarAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.ChipUidInvalido);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarFalha_QuandoPlacaNaoForEncontrada()
    {
        var slug = Guid.NewGuid().ToString("N");
        var command = new ConfirmarGravacaoPlacaCommand(slug, "04AABBCCDD");

        _placaRepository
            .ObterPorSlugParaAtualizacaoAsync(slug, Arg.Any<CancellationToken>())
            .Returns((Placa?)null);

        var result = await _useCase.ExecutarAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoEncontrada);
    }

    [Fact]
    public async Task ExecutarAsync_DeveGravarEAtualizar_QuandoPlacaEstiverElegivel()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug, "LOTE-01").Value!;
        placa.ReservarParaGravacao(TimeSpan.FromSeconds(90), "operador-01", DateTime.UtcNow);

        var command = new ConfirmarGravacaoPlacaCommand(slug, "04AABBCCDD", "operador-01");

        _placaRepository
            .ObterPorSlugParaAtualizacaoAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);

        var result = await _useCase.ExecutarAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Slug.Should().Be(slug);
        result.Value.ChipUid.Should().Be("04AABBCCDD");

        placa.StatusPlaca.Should().Be(StatusPlaca.Gravada);
        await _placaRepository.Received(1).AtualizarAsync(placa, Arg.Any<CancellationToken>());
    }
}
