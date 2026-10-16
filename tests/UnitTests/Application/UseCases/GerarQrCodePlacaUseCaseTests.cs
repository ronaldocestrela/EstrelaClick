using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public sealed class GerarQrCodePlacaUseCaseTests
{
    private readonly IPlacaRepository _placaRepository = Substitute.For<IPlacaRepository>();
    private readonly IQrCodeGenerator _qrCodeGenerator = Substitute.For<IQrCodeGenerator>();
    private readonly IOptions<QrCodeOptions> _options = Options.Create(new QrCodeOptions { BaseRedirecionamentoUrl = "https://estrela.click/r" });
    private readonly GerarQrCodePlacaUseCase _useCase;

    public GerarQrCodePlacaUseCaseTests()
    {
        _useCase = new GerarQrCodePlacaUseCase(_placaRepository, _qrCodeGenerator, _options);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("slug-com-hifens-invalido")]
    public async Task GerarPngPorSlugAsync_QuandoSlugInvalido_DeveRetornarFalhaSlugInvalido(string slugInvalido)
    {
        var result = await _useCase.GerarPngPorSlugAsync(slugInvalido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.SlugInvalido);
    }

    [Fact]
    public async Task GerarPngPorSlugAsync_QuandoPlacaNaoEncontrada_DeveRetornarFalhaNaoEncontrada()
    {
        var slug = Guid.NewGuid().ToString("N");
        _placaRepository.ObterPorSlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns((Placa?)null);

        var result = await _useCase.GerarPngPorSlugAsync(slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PlacaErrors.NaoEncontrada);
    }

    [Fact]
    public async Task GerarPngPorSlugAsync_QuandoPlacaExiste_DeveRetornarDtoComPngEContentType()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug, "LOTE-01").Value!;
        var pngBytes = new byte[] { 137, 80, 78, 71 };

        _placaRepository.ObterPorSlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);
        _qrCodeGenerator.GerarPng300Dpi($"https://estrela.click/r/{slug}", Arg.Any<int>())
            .Returns(Result<byte[]>.Success(pngBytes));

        var result = await _useCase.GerarPngPorSlugAsync(slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NomeArquivo.Should().Be($"{slug}.png");
        result.Value.ContentType.Should().Be("image/png");
        result.Value.Conteudo.Should().BeEquivalentTo(pngBytes);
    }

    [Fact]
    public async Task GerarSvgPorSlugAsync_QuandoPlacaExiste_DeveRetornarDtoComSvgEContentType()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug, "LOTE-01").Value!;
        var svgBytes = "<svg></svg>"u8.ToArray();

        _placaRepository.ObterPorSlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);
        _qrCodeGenerator.GerarSvg($"https://estrela.click/r/{slug}")
            .Returns(Result<byte[]>.Success(svgBytes));

        var result = await _useCase.GerarSvgPorSlugAsync(slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NomeArquivo.Should().Be($"{slug}.svg");
        result.Value.ContentType.Should().Be("image/svg+xml");
        result.Value.Conteudo.Should().BeEquivalentTo(svgBytes);
    }

    [Fact]
    public async Task GerarPngPorSlugAsync_QuandoGeradorFalhar_DeveRetornarFalhaGeracao()
    {
        var slug = Guid.NewGuid().ToString("N");
        var placa = Placa.Criar(slug, "LOTE-01").Value!;

        _placaRepository.ObterPorSlugAsync(slug, Arg.Any<CancellationToken>())
            .Returns(placa);
        _qrCodeGenerator.GerarPng300Dpi(Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result<byte[]>.Failure(QrCodeErrors.FalhaGeracao));

        var result = await _useCase.GerarPngPorSlugAsync(slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.FalhaGeracao);
    }
}
