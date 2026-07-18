using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class RetailerAliasConfiguration : IEntityTypeConfiguration<RetailerAlias>
{
    public void Configure(EntityTypeBuilder<RetailerAlias> builder)
    {
        builder.HasKey(ra => ra.Id);

        builder.Property(ra => ra.AliasName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(ra => ra.AliasName);

        // One-to-many relationship Retailer -> Aliases
        builder.HasOne(ra => ra.Retailer)
            .WithMany(r => r.Aliases)
            .HasForeignKey(ra => ra.RetailerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            // Amazon Aliases
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("a0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "Amazon" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("a0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "amazon.co.uk" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("a0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "Amazon UK" },

            // Currys Aliases
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "Currys" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "Currys PC World" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "PC World" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("c0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "currys.co.uk" },

            // Argos Aliases
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("b0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "Argos" },
            new RetailerAlias { Id = Guid.NewGuid(), RetailerId = Guid.Parse("b0f2b3ad-cc52-472e-8390-50d41f3d3281"), AliasName = "argos.co.uk" }
        );
    }
}
