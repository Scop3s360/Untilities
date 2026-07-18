using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class OfferTypeConfiguration : IEntityTypeConfiguration<OfferType>
{
    public void Configure(EntityTypeBuilder<OfferType> builder)
    {
        builder.HasKey(ot => ot.Id);

        builder.Property(ot => ot.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ot => ot.Description)
            .HasMaxLength(500);

        // Seed data
        builder.HasData(
            new OfferType { Id = Guid.Parse("11c223ad-cc52-472e-8390-50d41f3d3281"), Name = "Cashback", Description = "Return of a percentage of the amount spent to the consumer" },
            new OfferType { Id = Guid.Parse("22c223ad-cc52-472e-8390-50d41f3d3281"), Name = "Discount", Description = "Direct price reduction applied to purchase price" },
            new OfferType { Id = Guid.Parse("33c223ad-cc52-472e-8390-50d41f3d3281"), Name = "Reward Points", Description = "Loyalty system points earned per dollar/pound spent" },
            new OfferType { Id = Guid.Parse("44c223ad-cc52-472e-8390-50d41f3d3281"), Name = "Voucher", Description = "A discount code or certificate for specific promotions" },
            new OfferType { Id = Guid.Parse("55c223ad-cc52-472e-8390-50d41f3d3281"), Name = "Mortgage Cashback", Description = "Cashback specifically directed to pay down linked mortgage balances" }
        );
    }
}
