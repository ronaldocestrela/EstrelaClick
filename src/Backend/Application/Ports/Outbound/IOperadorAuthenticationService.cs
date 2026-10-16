using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public record OperadorInfo(string OperadorId, string Nome, bool Ativo);

public interface IOperadorAuthenticationService
{
    Task<Result<OperadorInfo>> ValidarCredenciaisAsync(string login, string senha, CancellationToken cancellationToken = default);
}
