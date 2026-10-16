using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IConfirmarGravacaoPlacaUseCase
{
    Task<Result<PlacaGravadaDto>> ExecutarAsync(ConfirmarGravacaoPlacaCommand command, CancellationToken cancellationToken = default);
}
