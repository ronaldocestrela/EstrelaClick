using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Inbound;

public interface IAutenticarOperadorUseCase
{
    Task<Result<AuthTokenResult>> ExecutarAsync(AutenticarOperadorCommand command, CancellationToken cancellationToken = default);
}
