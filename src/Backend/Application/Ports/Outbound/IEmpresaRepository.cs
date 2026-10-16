using EstrelaClick.Backend.Domain.Entities;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IEmpresaRepository
{
    Task<IReadOnlyList<Empresa>> ListarTodasAsync(CancellationToken cancellationToken = default);
    Task<Empresa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Empresa?> ObterPorIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task InserirAsync(Empresa empresa, CancellationToken cancellationToken = default);
    Task<int> ObterTotalPlacasPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default);
}
