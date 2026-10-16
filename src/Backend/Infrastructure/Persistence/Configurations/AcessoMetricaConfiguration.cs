using EstrelaClick.Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstrelaClick.Backend.Infrastructure.Persistence.Configurations;

internal sealed class AcessoMetricaConfiguration : IEntityTypeConfiguration<AcessoMetrica>
{
    public void Configure(EntityTypeBuilder<AcessoMetrica> builder)
    {
        builder.ToTable("AcessosMetricas");

        builder.HasKey(am => am.Id);

        builder.Property(am => am.PlacaId)
            .IsRequired();

        builder.Property(am => am.DataHoraUtc)
            .IsRequired();

        builder.Property(am => am.UserAgent)
            .HasMaxLength(512);

        builder.Property(am => am.IpHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(am => am.OrigemReferer)
            .HasMaxLength(2048);

        builder.HasIndex(am => new { am.PlacaId, am.DataHoraUtc })
            .HasDatabaseName("IX_AcessosMetricas_PlacaId_DataHoraUtc");

        builder.HasOne<Placa>()
            .WithMany()
            .HasForeignKey(am => am.PlacaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
