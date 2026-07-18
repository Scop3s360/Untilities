using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Application.Services.Acquisition;
using SaverSearch.Domain.Entities;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition;

public class OfferNormalisationServiceTests
{
    private readonly OfferNormalisationService _sut = new();

    private static readonly Guid RetailerId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid OfferTypeId = Guid.NewGuid();

    private static RawProviderOffer CreateRaw(
        string valueType = "percentage",
        decimal value = 5.0m,
        string externalId = "ext-100") => new(
        ExternalId: externalId,
        RetailerName: "Amazon",
        RetailerUrl: "https://www.amazon.co.uk",
        RetailerDomain: "amazon.co.uk",
        Title: "  5% Cashback  ",
        Description: "  Earn cashback  ",
        Terms: "  Min £10  ",
        OfferUrl: "  https://example.com/offer  ",
        ValueType: valueType,
        Value: value,
        MinimumSpend: 10.0m,
        MaximumReward: 50.0m,
        StartDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
        IsExclusive: true,
        RetrievedAt: DateTimeOffset.UtcNow,
        RawMetadata: new Dictionary<string, string>()
    );

    [Fact]
    public void Normalise_ShouldMapAllFields_Correctly()
    {
        var raw = CreateRaw();
        var offer = _sut.Normalise(raw, RetailerId, ProviderId, OfferTypeId);

        Assert.Equal(RetailerId, offer.RetailerId);
        Assert.Equal(ProviderId, offer.ProviderId);
        Assert.Equal(OfferTypeId, offer.OfferTypeId);
        Assert.Equal("ext-100", offer.ExternalId);
        Assert.Equal("5% Cashback", offer.Title);         // whitespace trimmed
        Assert.Equal("Earn cashback", offer.Description);  // whitespace trimmed
        Assert.Equal("Min £10", offer.Terms);
        Assert.Equal("https://example.com/offer", offer.OfferUrl);
        Assert.Equal(5.0m, offer.Value);
        Assert.Equal(10.0m, offer.MinimumSpend);
        Assert.Equal(50.0m, offer.MaximumReward);
        Assert.True(offer.IsExclusive);
        Assert.True(offer.IsActive);
    }

    [Theory]
    [InlineData("percentage", OfferValueType.Percentage)]
    [InlineData("cashback", OfferValueType.Percentage)]
    [InlineData("fixed", OfferValueType.FixedAmount)]
    [InlineData("fixedamount", OfferValueType.FixedAmount)]
    [InlineData("points", OfferValueType.Points)]
    [InlineData("other", OfferValueType.Other)]
    [InlineData("PERCENTAGE", OfferValueType.Percentage)]   // case-insensitive
    [InlineData("unknown_type", OfferValueType.Other)]       // unknown → Other
    [InlineData(null, OfferValueType.Other)]                 // null → Other
    public void Normalise_ShouldMapValueType_Correctly(string? rawType, OfferValueType expected)
    {
        var raw = CreateRaw(valueType: rawType ?? "percentage") with { ValueType = rawType! };
        var offer = _sut.Normalise(raw, RetailerId, ProviderId, OfferTypeId);
        Assert.Equal(expected, offer.ValueType);
    }

    [Fact]
    public void Normalise_ShouldProduceUniqueIds_ForEachOffer()
    {
        var raw1 = CreateRaw(externalId: "ext-A");
        var raw2 = CreateRaw(externalId: "ext-B");

        var offer1 = _sut.Normalise(raw1, RetailerId, ProviderId, OfferTypeId);
        var offer2 = _sut.Normalise(raw2, RetailerId, ProviderId, OfferTypeId);

        Assert.NotEqual(offer1.Id, offer2.Id);
        Assert.NotEqual(offer1.ExternalId, offer2.ExternalId);
    }
}
