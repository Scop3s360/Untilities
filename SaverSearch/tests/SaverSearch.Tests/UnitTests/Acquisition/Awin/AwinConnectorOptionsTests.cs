using SaverSearch.Infrastructure.Providers.Connectors.Awin;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition.Awin;

public class AwinConnectorOptionsTests
{
    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenPublisherIdIsZero()
    {
        var options = new AwinConnectorOptions { PublisherId = 0, AccessToken = "token" };
        Assert.False(options.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenAccessTokenIsEmpty()
    {
        var options = new AwinConnectorOptions { PublisherId = 12345, AccessToken = "" };
        Assert.False(options.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ShouldReturnFalse_WhenAccessTokenIsWhitespace()
    {
        var options = new AwinConnectorOptions { PublisherId = 12345, AccessToken = "   " };
        Assert.False(options.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ShouldReturnTrue_WhenBothCredentialsAreSet()
    {
        var options = new AwinConnectorOptions { PublisherId = 12345, AccessToken = "valid-token" };
        Assert.True(options.IsConfigured);
    }

    [Fact]
    public void SectionKey_ShouldBe_ExpectedValue()
    {
        Assert.Equal("Acquisition:Awin", AwinConnectorOptions.SectionKey);
    }

    [Fact]
    public void Defaults_ShouldMatch_ExpectedValues()
    {
        var options = new AwinConnectorOptions();
        Assert.Equal("https://api.awin.com", options.BaseUrl);
        Assert.Equal("GB", options.RegionCode);
        Assert.Equal(18, options.RateLimitPerMinute);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.False(options.Enabled); // Default: disabled until credentials set
    }
}
