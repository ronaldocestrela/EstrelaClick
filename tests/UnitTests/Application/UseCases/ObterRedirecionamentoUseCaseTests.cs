using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Shared.SharedKernel.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public sealed class ObterRedirecionamentoUseCaseTests
{
    private readonly IPlacaRedirecionamentoCache _cache = Substitute.For<IPlacaRedirecionamentoCache>();
    private readonly IMetricaAcessoQueue _metricaQueue = Substitute.For<IMetricaAcessoQueue>();
    private readonly ObterRedirecionamentoUseCase _useCase;

    public ObterRedirecionamentoUseCaseTests()
    {
        _useCase = new ObterRedirecionamentoUseCase(_cache, _metricaQueue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalido")]
    [InlineData("12345678-1234-1234-1234-123456789abc")] // Guid com hífens não é formato N
    public async Task ExecutarAsync_QuandoSlugForInvalido_DeveRetornarFalhaSlugInvalido(string slugInvalido)
    {
        // Act
        var result = await _useCase.ExecutarAsync(slugInvalido, "UserAgent", "hash123", "https://google.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.SlugInvalido);
        _metricaQueue.DidNotReceiveWithAnyArgs().TryEnqueue(default!);
        await _cache.DidNotReceiveWithAnyArgs().ObterPorSlugAsync(default!, default);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPlacaNaoExistir_DeveRetornarFalhaNaoEncontrada()
    {
        // Arrange
        var validSlug = Guid.NewGuid().ToString("N");
        _cache.ObterPorSlugAsync(validSlug, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PlacaCacheItem?>((PlacaCacheItem?)null));

        // Act
        var result = await _useCase.ExecutarAsync(validSlug, "UserAgent", "hash123", "https://google.com");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoEncontrada);
        _metricaQueue.DidNotReceiveWithAnyArgs().TryEnqueue(default!);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPlacaEstiverComStatusCriada_DeveRetornarFalhaNaoEncontrada()
    {
        // Arrange
        var validSlug = Guid.NewGuid().ToString("N");
        var item = new PlacaCacheItem(Guid.NewGuid(), validSlug, StatusPlaca.Criada, null);
        _cache.ObterPorSlugAsync(validSlug, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PlacaCacheItem?>(item));

        // Act
        var result = await _useCase.ExecutarAsync(validSlug, "UserAgent", "hash123", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoEncontrada);
        _metricaQueue.DidNotReceiveWithAnyArgs().TryEnqueue(default!);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPlacaEstiverComStatusGravada_DeveRetornarFalhaNaoAtivada()
    {
        // Arrange
        var validSlug = Guid.NewGuid().ToString("N");
        var item = new PlacaCacheItem(Guid.NewGuid(), validSlug, StatusPlaca.Gravada, null);
        _cache.ObterPorSlugAsync(validSlug, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PlacaCacheItem?>(item));

        // Act
        var result = await _useCase.ExecutarAsync(validSlug, "UserAgent", "hash123", null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoAtivada);
        _metricaQueue.DidNotReceiveWithAnyArgs().TryEnqueue(default!);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPlacaEstiverVinculada_DeveRetornarSucessoEEnfileirarMetrica()
    {
        // Arrange
        var validSlug = Guid.NewGuid().ToString("N");
        var placaId = Guid.NewGuid();
        const string urlDestino = "https://instagram.com/minhaloja";
        var item = new PlacaCacheItem(placaId, validSlug, StatusPlaca.Vinculada, urlDestino);

        _cache.ObterPorSlugAsync(validSlug, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PlacaCacheItem?>(item));
        _metricaQueue.TryEnqueue(Arg.Any<MetricaAcessoEvent>()).Returns(true);

        // Act
        var result = await _useCase.ExecutarAsync(
            validSlug,
            "Mozilla/5.0 Chrome",
            "hash-ip-sha256",
            "https://qr.tag",
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PlacaId.Should().Be(placaId);
        result.Value.UrlDestino.Should().Be(urlDestino);

        _metricaQueue.Received(1).TryEnqueue(Arg.Is<MetricaAcessoEvent>(m =>
            m.PlacaId == placaId &&
            m.UserAgent == "Mozilla/5.0 Chrome" &&
            m.IpHash == "hash-ip-sha256" &&
            m.OrigemReferer == "https://qr.tag" &&
            m.DataHoraUtc <= DateTime.UtcNow));
    }

    [Fact]
    public async Task ExecutarAsync_QuandoPlacaVinculadaMasFilaCheia_AindaDeveRetornarSucessoComUrl()
    {
        // Arrange
        var validSlug = Guid.NewGuid().ToString("N");
        var placaId = Guid.NewGuid();
        const string urlDestino = "https://instagram.com/minhaloja";
        var item = new PlacaCacheItem(placaId, validSlug, StatusPlaca.Vinculada, urlDestino);

        _cache.ObterPorSlugAsync(validSlug, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PlacaCacheItem?>(item));
        _metricaQueue.TryEnqueue(Arg.Any<MetricaAcessoEvent>()).Returns(false); // Fila cheia / drop

        // Act
        var result = await _useCase.ExecutarAsync(
            validSlug,
            null,
            "hash-ip",
            null,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.UrlDestino.Should().Be(urlDestino);
        _metricaQueue.Received(1).TryEnqueue(Arg.Any<MetricaAcessoEvent>());
    }
}
