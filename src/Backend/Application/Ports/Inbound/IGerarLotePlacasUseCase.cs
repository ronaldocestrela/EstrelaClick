using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IGerarLotePlacasUseCase
{
    Task<Result<LotePlacasCriadoDto>> ExecutarAsync(GerarLotePlacasCommand command, CancellationToken cancellationToken = default);
}
