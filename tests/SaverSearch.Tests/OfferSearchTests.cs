using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests;

public class OfferSearchTests
{
    [Fact]
    public void Offer_ShouldInitializeCorrectly()
    {
        // Arrange
        var category = new Category
        {
            Name = "Electronics"
        };

        var retailer = new Retailer
        {
            Name = "Amazon",
            Slug = "amazon",
            Website = "https://amazon.co.uk",
            Category = category
        };

        var provider = new Provider
        {
            Name = "TopCashback",
            Website = "https://topcashback.co.uk"
        };

        var offerType = new OfferType
        {
            Name = "Cashback"
        };

        var offer = new Offer
        {
            Title = "10% Cashback",
            OfferUrl = "https://amazon.co.uk/deals",
            Provider = provider,
            OfferType = offerType,
            Value = 10.0m,
            ValueType = OfferValueType.Percentage,
            Retailer = retailer
        };

        // Assert
        Assert.Equal("10% Cashback", offer.Title);
        Assert.Equal("Amazon", offer.Retailer.Name);
    }
}
