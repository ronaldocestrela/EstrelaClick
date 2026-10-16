using EstrelaClick.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstrelaClick.Backend.Infrastructure.Persistence.Configurations;

internal sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("Empresas");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RazaoSocial)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.Telefone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.IdentityUserId)
            .HasMaxLength(450);

        builder.Property(e => e.StatusAtivo)
            .IsRequired();

        builder.Property(e => e.DataCadastroUtc)
            .IsRequired();
    }
}
