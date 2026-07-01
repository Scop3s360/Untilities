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

        // Seed default retailers
        builder.HasData(
            new Retailer
            {
                Id = Guid.Parse("a0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                Name = "Amazon",
                Slug = "amazon",
                Website = "https://amazon.co.uk",
                CategoryId = Guid.Parse("23a2a3ad-cc52-472e-8390-50d41f3d3281"),
                IsActive = true
            },
            new Retailer
            {
                Id = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                Name = "Currys",
                Slug = "currys",
                Website = "https://currys.co.uk",
                CategoryId = Guid.Parse("23a2a3ad-cc52-472e-8390-50d41f3d3281"),
                IsActive = true
            },
            new Retailer
            {
                Id = Guid.Parse("b0f2b3ad-cc52-472e-8390-50d41f3d3281"),
                Name = "Argos",
                Slug = "argos",
                Website = "https://argos.co.uk",
                CategoryId = Guid.Parse("23a2a3ad-cc52-472e-8390-50d41f3d3281"),
                IsActive = true
            }
        );
    }
}
