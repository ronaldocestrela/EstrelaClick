using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IObterResumoLoteUseCase
{
    Task<Result<LoteResumoDto>> ExecutarAsync(string numeroLote, CancellationToken cancellationToken = default);
}
