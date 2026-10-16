using EstrelaClick.Backend.Application.Ports.Outbound;
using EstrelaClick.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EstrelaClick.Backend.Infrastructure.Persistence.Repositories;

public sealed class EmpresaRepository : IEmpresaRepository
{
    private readonly EstrelaClickDbContext _dbContext;

    public EmpresaRepository(EstrelaClickDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Empresa>> ListarTodasAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Empresas
            .AsNoTracking()
            .OrderByDescending(e => e.DataCadastroUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Empresa?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Empresa?> ObterPorIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId, cancellationToken);
    }

    public async Task InserirAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        await _dbContext.Empresas.AddAsync(empresa, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ObterTotalPlacasPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Placas
            .AsNoTracking()
            .CountAsync(p => p.EmpresaId == empresaId, cancellationToken);
    }
}
