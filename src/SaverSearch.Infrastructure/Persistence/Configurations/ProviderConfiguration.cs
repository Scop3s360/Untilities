using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Website)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.LogoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Seed data
        builder.HasData(
            new Provider { Id = Guid.Parse("11b223ad-cc52-472e-8390-50d41f3d3281"), Name = "TopCashback", Website = "https://www.topcashback.co.uk", Description = "UK's highest paying cashback site", IsActive = true },
            new Provider { Id = Guid.Parse("22b223ad-cc52-472e-8390-50d41f3d3281"), Name = "Quidco", Website = "https://www.quidco.com", Description = "Great rates and easy cashback search", IsActive = true },
            new Provider { Id = Guid.Parse("33b223ad-cc52-472e-8390-50d41f3d3281"), Name = "Sprive", Website = "https://sprive.com", Description = "Smart app offering cashback used to pay down mortgages", IsActive = true },
            new Provider { Id = Guid.Parse("44b223ad-cc52-472e-8390-50d41f3d3281"), Name = "Barclaycard", Website = "https://www.barclaycard.co.uk", Description = "Credit card reward schemes and retailer cashback partnerships", IsActive = true },
            new Provider { Id = Guid.Parse("55b223ad-cc52-472e-8390-50d41f3d3281"), Name = "Chase", Website = "https://www.chase.co.uk", Description = "1% cashback on eligible everyday debit card spending", IsActive = true }
        );
    }
}
