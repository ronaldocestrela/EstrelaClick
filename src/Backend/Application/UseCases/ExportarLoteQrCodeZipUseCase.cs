using EstrelaClick.Backend.Application.Common.Errors;
using EstrelaClick.Backend.Application.Common.Options;
using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Backend.Application.Ports.Inbound;
using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Shared.SharedKernel.Results;
using Microsoft.Extensions.Options;

namespace EstrelaClick.Backend.Application.UseCases;

public sealed class ExportarLoteQrCodeZipUseCase : IExportarLoteQrCodeZipUseCase
{
    private readonly IPlacaRepository _placaRepository;
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IZipArchiveService _zipArchiveService;
    private readonly QrCodeOptions _options;

    public ExportarLoteQrCodeZipUseCase(
        IPlacaRepository placaRepository,
        IQrCodeGenerator qrCodeGenerator,
        IZipArchiveService zipArchiveService,
        IOptions<QrCodeOptions> options)
    {
        _placaRepository = placaRepository;
        _qrCodeGenerator = qrCodeGenerator;
        _zipArchiveService = zipArchiveService;
        _options = options.Value;
    }

    public async Task<Result<LoteExportadoZipDto>> ExecutarAsync(string numeroLote, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(numeroLote))
        {
            return Result<LoteExportadoZipDto>.Failure(QrCodeErrors.LoteObrigatorio);
        }

        var loteTrimmed = numeroLote.Trim();
        var placas = await _placaRepository.ObterPorLoteAsync(loteTrimmed, cancellationToken);
        if (placas is null || placas.Count == 0)
        {
            return Result<LoteExportadoZipDto>.Failure(QrCodeErrors.LoteNaoEncontrado);
        }

        var itens = new List<(string Slug, string UrlRedirecionamento, byte[] PngConteudo, byte[] SvgConteudo)>(placas.Count);

        var baseUrl = _options.BaseRedirecionamentoUrl.TrimEnd('/');

        foreach (var placa in placas)
        {
            var url = $"{baseUrl}/{placa.Slug}";

            var pngResult = _qrCodeGenerator.GerarPng300Dpi(url);
            if (pngResult.IsFailure)
            {
                return Result<LoteExportadoZipDto>.Failure(pngResult.Error);
            }

            var svgResult = _qrCodeGenerator.GerarSvg(url);
            if (svgResult.IsFailure)
            {
                return Result<LoteExportadoZipDto>.Failure(svgResult.Error);
            }

            itens.Add((placa.Slug, url, pngResult.Value!, svgResult.Value!));
        }

        var zipResult = _zipArchiveService.CriarPacoteLote(loteTrimmed, itens);
        if (zipResult.IsFailure)
        {
            return Result<LoteExportadoZipDto>.Failure(zipResult.Error);
        }

        return Result<LoteExportadoZipDto>.Success(new LoteExportadoZipDto(
            NomeArquivo: $"lote-{loteTrimmed}.zip",
            Conteudo: zipResult.Value!,
            TotalPlacas: placas.Count));
    }
}
