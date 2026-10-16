using EstrelaClick.Backend.Domain.Entities;
using EstrelaClick.Shared.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstrelaClick.Backend.Infrastructure.Persistence.Configurations;

internal sealed class PlacaConfiguration : IEntityTypeConfiguration<Placa>
{
    public void Configure(EntityTypeBuilder<Placa> builder)
    {
        builder.ToTable("Placas");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(32)
            .IsFixedLength();

        builder.HasIndex(p => p.Slug)
            .HasDatabaseName("IX_Placas_Slug")
            .IsUnique();

        builder.Property(p => p.NumeroLote)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("PADRAO");

        builder.HasIndex(p => p.NumeroLote)
            .HasDatabaseName("IX_Placas_NumeroLote");

        builder.Property(p => p.UrlDestino)
            .HasMaxLength(2048);

        builder.Property(p => p.ChipUid)
            .HasMaxLength(64);

        builder.Property(p => p.StatusPlaca)
            .HasConversion<int>()
            .HasDefaultValue(StatusPlaca.Criada)
            .IsRequired();

        builder.Property(p => p.DataCriacaoUtc)
            .IsRequired();

        builder.Property(p => p.DataReservaUtc);

        builder.Property(p => p.OperadorReservaId)
            .HasMaxLength(64);

        builder.HasIndex(p => new { p.StatusPlaca, p.NumeroLote, p.DataCriacaoUtc })
            .HasDatabaseName("IX_Placas_FilaPendentes");

        builder.Property(p => p.DataGravacaoUtc);

        builder.Property(p => p.DataAtivacaoUtc);

        builder.Property(p => p.EmpresaId);

        builder.Property(p => p.FiltroSatisfacaoAtivo)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.UrlSacInterno)
            .HasMaxLength(2048);

        builder.HasOne<Empresa>()
            .WithMany()
            .HasForeignKey(p => p.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
