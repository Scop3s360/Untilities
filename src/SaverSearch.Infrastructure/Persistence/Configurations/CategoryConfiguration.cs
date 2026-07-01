using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // One-to-many relationship Category -> Retailers
        builder.HasMany(c => c.Retailers)
            .WithOne(r => r.Category)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data
        builder.HasData(
            new Category { Id = Guid.Parse("23a2a3ad-cc52-472e-8390-50d41f3d3281"), Name = "Electronics", Description = "TVs, laptops, phones, and smart home appliances", IsActive = true },
            new Category { Id = Guid.Parse("8b72de70-c081-420a-9d62-f6cb3ce183a3"), Name = "Supermarkets", Description = "Groceries, weekly shops, and household essentials", IsActive = true },
            new Category { Id = Guid.Parse("501bd346-63e8-466d-b873-67ca2b6eb2b5"), Name = "Fashion", Description = "Clothing, footwear, and accessories", IsActive = true },
            new Category { Id = Guid.Parse("3d64c148-d3c5-4148-912f-db4fe72b0c95"), Name = "Home", Description = "Furniture, garden, and home improvement decor", IsActive = true },
            new Category { Id = Guid.Parse("9a8cd34a-9ef8-4ca7-9e7f-b67f2e1a3bc8"), Name = "Travel", Description = "Hotels, flights, holidays, and transport", IsActive = true }
        );
    }
}
