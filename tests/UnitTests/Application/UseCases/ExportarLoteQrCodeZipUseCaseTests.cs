using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Application.UseCases;
using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Results;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace EstrelaClick.Tests.UnitTests.Application.UseCases;

public sealed class ExportarLoteQrCodeZipUseCaseTests
{
    private readonly IPlacaRepository _placaRepository = Substitute.For<IPlacaRepository>();
    private readonly IQrCodeGenerator _qrCodeGenerator = Substitute.For<IQrCodeGenerator>();
    private readonly IZipArchiveService _zipArchiveService = Substitute.For<IZipArchiveService>();
    private readonly IOptions<QrCodeOptions> _options = Options.Create(new QrCodeOptions { BaseRedirecionamentoUrl = "https://estrela.click/r" });
    private readonly ExportarLoteQrCodeZipUseCase _useCase;

    public ExportarLoteQrCodeZipUseCaseTests()
    {
        _useCase = new ExportarLoteQrCodeZipUseCase(
            _placaRepository,
            _qrCodeGenerator,
            _zipArchiveService,
            _options);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecutarAsync_QuandoNumeroLoteForVazio_DeveRetornarFalhaLoteObrigatorio(string loteInvalido)
    {
        var result = await _useCase.ExecutarAsync(loteInvalido);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.LoteObrigatorio);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLoteNaoPossuirPlacas_DeveRetornarFalhaLoteNaoEncontrado()
    {
        _placaRepository.ObterPorLoteAsync("LOTE-INEXISTENTE", Arg.Any<CancellationToken>())
            .Returns(new List<Placa>());

        var result = await _useCase.ExecutarAsync("LOTE-INEXISTENTE");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.LoteNaoEncontrado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoLotePossuirPlacas_DeveRetornarDtoComZipEContagem()
    {
        var lote = "LOTE-2026-01";
        var slug1 = Guid.NewGuid().ToString("N");
        var slug2 = Guid.NewGuid().ToString("N");

        var placa1 = Placa.Criar(slug1, lote).Value!;
        var placa2 = Placa.Criar(slug2, lote).Value!;
        var listaPlacas = new List<Placa> { placa1, placa2 };

        _placaRepository.ObterPorLoteAsync(lote, Arg.Any<CancellationToken>())
            .Returns(listaPlacas);

        _qrCodeGenerator.GerarPng300Dpi(Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result<byte[]>.Success(new byte[] { 1, 2, 3 }));

        _qrCodeGenerator.GerarSvg(Arg.Any<string>())
            .Returns(Result<byte[]>.Success(new byte[] { 4, 5, 6 }));

        var zipBytes = new byte[] { 80, 75, 3, 4 };
        _zipArchiveService.CriarPacoteLote(lote, Arg.Any<IReadOnlyList<(string, string, byte[], byte[])>>())
            .Returns(Result<byte[]>.Success(zipBytes));

        var result = await _useCase.ExecutarAsync(lote);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.NomeArquivo.Should().Be($"lote-{lote}.zip");
        result.Value.TotalPlacas.Should().Be(2);
        result.Value.Conteudo.Should().BeEquivalentTo(zipBytes);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoGeradorQrFalhar_DeveRetornarFalhaGeracao()
    {
        var lote = "LOTE-2026-01";
        var slug1 = Guid.NewGuid().ToString("N");
        var placa1 = Placa.Criar(slug1, lote).Value!;

        _placaRepository.ObterPorLoteAsync(lote, Arg.Any<CancellationToken>())
            .Returns(new List<Placa> { placa1 });

        _qrCodeGenerator.GerarPng300Dpi(Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result<byte[]>.Failure(QrCodeErrors.FalhaGeracao));

        var result = await _useCase.ExecutarAsync(lote);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(QrCodeErrors.FalhaGeracao);
    }
}
