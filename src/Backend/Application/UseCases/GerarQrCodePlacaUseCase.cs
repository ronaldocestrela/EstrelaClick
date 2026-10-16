using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.Extensions.Options;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class GerarQrCodePlacaUseCase : IGerarQrCodePlacaUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly QrCodeOptions _options;

    public GerarQrCodePlacaUseCase(
        IPlacaRepository placaRepository,
        IQrCodeGenerator qrCodeGenerator,
        IOptions<QrCodeOptions> options)
    {
        _placaRepository = placaRepository;
        _qrCodeGenerator = qrCodeGenerator;
        _options = options.Value;
    }

    public async Task<Result<QrCodeArquivoDto>> GerarPngPorSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var validacao = ValidarSlug(slug);
        if (validacao.IsFailure)
        {
            return Result<QrCodeArquivoDto>.Failure(validacao.Error);
        }

        var placa = await _placaRepository.ObterPorSlugAsync(slug, cancellationToken);
        if (placa is null)
        {
            return Result<QrCodeArquivoDto>.Failure(PlacaErrors.NaoEncontrada);
        }

        var url = ObterUrlRedirecionamento(slug);
        var qrResult = _qrCodeGenerator.GerarPng300Dpi(url);
        if (qrResult.IsFailure)
        {
            return Result<QrCodeArquivoDto>.Failure(qrResult.Error);
        }

        return Result<QrCodeArquivoDto>.Success(new QrCodeArquivoDto(
            NomeArquivo: $"{slug}.png",
            Conteudo: qrResult.Value!,
            ContentType: "image/png"));
    }

    public async Task<Result<QrCodeArquivoDto>> GerarSvgPorSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var validacao = ValidarSlug(slug);
        if (validacao.IsFailure)
        {
            return Result<QrCodeArquivoDto>.Failure(validacao.Error);
        }

        var placa = await _placaRepository.ObterPorSlugAsync(slug, cancellationToken);
        if (placa is null)
        {
            return Result<QrCodeArquivoDto>.Failure(PlacaErrors.NaoEncontrada);
        }

        var url = ObterUrlRedirecionamento(slug);
        var qrResult = _qrCodeGenerator.GerarSvg(url);
        if (qrResult.IsFailure)
        {
            return Result<QrCodeArquivoDto>.Failure(qrResult.Error);
        }

        return Result<QrCodeArquivoDto>.Success(new QrCodeArquivoDto(
            NomeArquivo: $"{slug}.svg",
            Conteudo: qrResult.Value!,
            ContentType: "image/svg+xml"));
    }

    private static Result ValidarSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug) || !Guid.TryParseExact(slug, "N", out _))
        {
            return Result.Failure(PlacaErrors.SlugInvalido);
        }

        return Result.Success();
    }

    private string ObterUrlRedirecionamento(string slug)
    {
        var baseUrl = _options.BaseRedirecionamentoUrl.TrimEnd('/');
        return $"{baseUrl}/{slug}";
    }
}
