using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IExportarLoteQrCodeZipUseCase
{
    Task<Result<LoteExportadoZipDto>> ExecutarAsync(string numeroLote, CancellationToken cancellationToken = default);
}
