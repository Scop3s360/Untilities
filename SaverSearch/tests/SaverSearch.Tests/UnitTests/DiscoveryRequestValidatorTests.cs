using SaverSearch.Application.Dtos.Discovery;
using Xunit;

namespace SaverSearch.Tests.UnitTests;

public class DiscoveryRequestValidatorTests
{
    private readonly DiscoveryRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WithValidMinimalRequest()
    {
        var request = new DiscoveryRequest { Query = "amazon" };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldFail_WhenQueryIsEmpty()
    {
        var request = new DiscoveryRequest { Query = "" };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validate_ShouldFail_WhenQueryIsSingleCharacter()
    {
        var request = new DiscoveryRequest { Query = "a" };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validate_ShouldFail_WhenQueryExceeds500Characters()
    {
        var request = new DiscoveryRequest { Query = new string('x', 501) };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSpendAmountIsNegative()
    {
        var request = new DiscoveryRequest { Query = "amazon", SpendAmount = -10.0m };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SpendAmount");
    }

    [Fact]
    public void Validate_ShouldPass_WhenSpendAmountIsZero()
    {
        var request = new DiscoveryRequest { Query = "amazon", SpendAmount = 0m };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenSpendAmountIsNull()
    {
        var request = new DiscoveryRequest { Query = "amazon", SpendAmount = null };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WithAllOptionalFieldsPopulated()
    {
        var request = new DiscoveryRequest
        {
            Query = "cashback on amazon",
            SpendAmount = 250.00m,
            UserGoal = UserGoal.MaximumSavings,
            RetailerSlug = "amazon",
            UserRegion = "GB",
            PaymentMethod = "AmexGold"
        };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
