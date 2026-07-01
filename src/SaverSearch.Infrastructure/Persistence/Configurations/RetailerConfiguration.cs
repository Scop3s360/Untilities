using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class RetailerConfiguration : IEntityTypeConfiguration<Retailer>
{
    public void Configure(EntityTypeBuilder<Retailer> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(r => r.Slug)
            .IsUnique();

        builder.Property(r => r.Website)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(r => r.LogoUrl)
            .HasMaxLength(500);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.UpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
