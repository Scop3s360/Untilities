using SaverSearch.Application.Common.Models.Acquisition;
using SaverSearch.Application.Services.Acquisition;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition;

public class OfferValidationServiceTests
{
    private readonly OfferValidationService _sut = new();
    private const string Provider = "TestProvider";

    private static RawProviderOffer ValidOffer(string externalId = "ext-001") => new(
        ExternalId: externalId,
        RetailerName: "Amazon",
        RetailerUrl: "https://www.amazon.co.uk",
        RetailerDomain: "amazon.co.uk",
        Title: "5% Cashback",
        Description: null,
        Terms: null,
        OfferUrl: "https://example.com/offer",
        ValueType: "percentage",
        Value: 5.0m,
        MinimumSpend: null,
        MaximumReward: null,
        StartDate: null,
        EndDate: null,
        IsExclusive: false,
        RetrievedAt: DateTimeOffset.UtcNow,
        RawMetadata: new Dictionary<string, string>()
    );

    [Fact]
    public void Validate_ShouldPass_WithValidOffer()
    {
        var result = _sut.Validate(ValidOffer(), Provider);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldFail_WhenExternalIdIsEmpty()
    {
        var offer = ValidOffer() with { ExternalId = "" };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ExternalId is required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRetailerNameIsEmpty()
    {
        var offer = ValidOffer() with { RetailerName = "" };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("RetailerName is required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueIsZero()
    {
        var offer = ValidOffer() with { Value = 0m };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Value must be greater than zero"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenValueIsNegative()
    {
        var offer = ValidOffer() with { Value = -5m };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Value must be greater than zero"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenEndDateIsBeforeStartDate()
    {
        var offer = ValidOffer() with
        {
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(1)
        };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("EndDate") && e.Contains("StartDate"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        var offer = ValidOffer() with { Title = "" };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Title is required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenOfferUrlIsEmpty()
    {
        var offer = ValidOffer() with { OfferUrl = "" };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("OfferUrl is required"));
    }

    [Fact]
    public void Validate_ShouldAddWarning_WhenOfferUrlIsRelative()
    {
        var offer = ValidOffer() with { OfferUrl = "/relative/path" };
        var result = _sut.Validate(offer, Provider);
        // Still valid (only a warning)
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "OfferUrl");
    }

    [Fact]
    public void Validate_ShouldAddWarning_WhenValueTypeIsUnknown()
    {
        var offer = ValidOffer() with { ValueType = "mystery_type" };
        var result = _sut.Validate(offer, Provider);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "ValueType");
    }

    [Fact]
    public void Validate_ShouldAddWarning_WhenRetailerNameIsPlaceholder()
    {
        var offer = ValidOffer() with { RetailerName = "Test" };
        var result = _sut.Validate(offer, Provider);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "RetailerName");
    }

    [Fact]
    public void Validate_ShouldAddWarning_WhenEndDateIsInPast()
    {
        var offer = ValidOffer() with { EndDate = DateTime.UtcNow.AddDays(-1) };
        var result = _sut.Validate(offer, Provider);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "EndDate");
    }

    [Fact]
    public void Validate_ShouldAccumulate_MultipleErrors()
    {
        var offer = ValidOffer() with { ExternalId = "", Value = 0m, Title = "" };
        var result = _sut.Validate(offer, Provider);
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3);
    }
}
