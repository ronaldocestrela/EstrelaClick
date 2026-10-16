using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IGerarQrCodePlacaUseCase
{
    Task<Result<QrCodeArquivoDto>> GerarPngPorSlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<QrCodeArquivoDto>> GerarSvgPorSlugAsync(string slug, CancellationToken cancellationToken = default);
}
