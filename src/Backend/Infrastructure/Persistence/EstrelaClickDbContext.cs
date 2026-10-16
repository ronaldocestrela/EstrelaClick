using EstrelaClick.Backend.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EstrelaClick.Backend.Infrastructure.Persistence;

public sealed class EstrelaClickDbContext(DbContextOptions<EstrelaClickDbContext> options)
    : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Placa> Placas => Set<Placa>();

    public DbSet<AcessoMetrica> AcessosMetricas => Set<AcessoMetrica>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("estrelaclick");

        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(EstrelaClickDbContext).Assembly);
    }
}
