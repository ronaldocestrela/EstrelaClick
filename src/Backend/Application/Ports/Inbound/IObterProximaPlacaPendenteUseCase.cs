using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IObterProximaPlacaPendenteUseCase
{
    Task<Result<PlacaPendenteDto>> ExecutarAsync(ObterProximaPlacaPendenteQuery query, CancellationToken cancellationToken = default);
}
