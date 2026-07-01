using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Description)
            .HasMaxLength(2000);

        builder.Property(o => o.Value)
            .HasPrecision(18, 4);

        // Map enum to string in SQLite DB
        builder.Property(o => o.ValueType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.MinimumSpend)
            .HasPrecision(18, 4);

        builder.Property(o => o.MaximumReward)
            .HasPrecision(18, 4);

        builder.Property(o => o.Terms)
            .HasMaxLength(4000);

        builder.Property(o => o.OfferUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(o => o.ExternalId)
            .HasMaxLength(500);

        builder.HasIndex(o => new { o.ProviderId, o.ExternalId });

        builder.Property(o => o.IsExclusive)
            .HasDefaultValue(false);

        builder.Property(o => o.IsActive)
            .HasDefaultValue(true);

        builder.Property(o => o.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasOne(o => o.Retailer)
            .WithMany(r => r.Offers)
            .HasForeignKey(o => o.RetailerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Provider)
            .WithMany(p => p.Offers)
            .HasForeignKey(o => o.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.OfferType)
            .WithMany(ot => ot.Offers)
            .HasForeignKey(o => o.OfferTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
