using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class ImportJobRecordConfiguration : IEntityTypeConfiguration<ImportJobRecord>
{
    public void Configure(EntityTypeBuilder<ImportJobRecord> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.ProviderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.ConnectorVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(j => j.Warnings)
            .HasMaxLength(16000);

        builder.Property(j => j.StartedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(j => j.ProviderName);
        builder.HasIndex(j => j.StartedAt);
    }
}
