using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IObterRedirecionamentoUseCase
{
    Task<Result<RedirecionamentoDto>> ExecutarAsync(
        string slug,
        string? userAgent,
        string ipHash,
        string? referer,
        CancellationToken cancellationToken = default);
}
