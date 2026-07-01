using SaverSearch.Infrastructure.Providers.Connectors.Awin;
using Xunit;

namespace SaverSearch.Tests.UnitTests.Acquisition.Awin;

public class AwinOfferMapperTests
{
    // ── Test Data Helpers ────────────────────────────────────────────────────

    private static AwinProgramme MakeProgramme(int id = 1, string name = "Amazon") => new()
    {
        Id = id,
        Name = name,
        DisplayUrl = $"https://www.{name.ToLower()}.co.uk",
        ClickThroughUrl = $"https://www.awin1.com/cread.php?awinmid={id}",
        LogoUrl = $"https://cdn.awin.com/{name.ToLower()}.png",
        PrimaryRegion = new AwinRegion { Name = "United Kingdom", CountryCode = "GB" },
        PrimarySector = new AwinSector { Name = "Electronics" },
        Relationship = "joined"
    };

    private static AwinPromotion MakePromotion(
        int advertiserId = 1,
        string? description = null,
        decimal percentage = 5.0m,
        decimal? fixedAmount = null) => new()
    {
        Id = 123456,
        AdvertiserId = advertiserId,
        AdvertiserName = "Amazon",
        Description = description,
        Code = null,
        Type = "cashback",
        Exclusive = false,
        Terms = "Min spend £10",
        StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CommissionGroups = new List<AwinCommissionGroup>
        {
            new()
            {
                Name = "cashback",
                Type = "percentage",
                Percentage = fixedAmount == null ? percentage : null,
                Amount = fixedAmount.HasValue
                    ? new AwinCommissionAmount { Value = fixedAmount.Value, Currency = "GBP" }
                    : null
            }
        }
    };

    // ── Core Mapping Tests ───────────────────────────────────────────────────

    [Fact]
    public void Map_ShouldProduceOneOffer_WhenPromotionMatchesProgramme()
    {
        var programmes = new[] { MakeProgramme(1, "Amazon") };
        var promotions = new[] { MakePromotion(advertiserId: 1) };

        var offers = AwinOfferMapper.Map(promotions, programmes).ToList();

        Assert.Single(offers);
        Assert.Equal("Amazon", offers[0].RetailerName);
        Assert.Equal("123456", offers[0].ExternalId);
        Assert.Equal(5.0m, offers[0].Value);
        Assert.Equal("percentage", offers[0].ValueType);
    }

    [Fact]
    public void Map_ShouldSkip_WhenPromotionHasNoMatchingProgramme()
    {
        var programmes = new[] { MakeProgramme(id: 1) };
        var promotions = new[] { MakePromotion(advertiserId: 999) }; // no match

        var offers = AwinOfferMapper.Map(promotions, programmes).ToList();

        Assert.Empty(offers);
    }

    [Fact]
    public void Map_ShouldSkip_WhenCommissionValueIsZero()
    {
        var prog = MakeProgramme(1);
        var promo = MakePromotion(advertiserId: 1, percentage: 0m);

        var offers = AwinOfferMapper.Map(new[] { promo }, new[] { prog }).ToList();

        Assert.Empty(offers);
    }

    [Fact]
    public void Map_ShouldSkip_WhenNoCommissionGroups()
    {
        var prog = MakeProgramme(1);
        var promo = MakePromotion(advertiserId: 1);
        promo.CommissionGroups = null;

        var offers = AwinOfferMapper.Map(new[] { promo }, new[] { prog }).ToList();

        Assert.Empty(offers);
    }

    // ── Value Type Mapping ───────────────────────────────────────────────────

    [Fact]
    public void Map_ShouldSetValueType_Percentage_WhenPercentageSet()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1, percentage: 7.5m) },
            new[] { MakeProgramme(1) }).ToList();

        Assert.Equal("percentage", offers[0].ValueType);
        Assert.Equal(7.5m, offers[0].Value);
    }

    [Fact]
    public void Map_ShouldSetValueType_Fixed_WhenFixedAmountSet()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1, fixedAmount: 10.0m) },
            new[] { MakeProgramme(1) }).ToList();

        Assert.Equal("fixed", offers[0].ValueType);
        Assert.Equal(10.0m, offers[0].Value);
    }

    // ── Title Generation ─────────────────────────────────────────────────────

    [Fact]
    public void Map_ShouldUseDescription_AsTitle_WhenAvailable()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1, description: "Exclusive 5% cashback on all orders") },
            new[] { MakeProgramme(1, "Amazon") }).ToList();

        Assert.Equal("Exclusive 5% cashback on all orders", offers[0].Title);
    }

    [Fact]
    public void Map_ShouldSynthesiseTitle_WhenDescriptionIsNull()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1, description: null, percentage: 3.5m) },
            new[] { MakeProgramme(1, "Tesco") }).ToList();

        Assert.Contains("3.5%", offers[0].Title);
        Assert.Contains("Tesco", offers[0].Title);
    }

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void Map_ShouldInclude_NetworkMetadata()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1) },
            new[] { MakeProgramme(1) }).ToList();

        Assert.True(offers[0].RawMetadata.ContainsKey("network"));
        Assert.Equal("AWIN", offers[0].RawMetadata["network"]);
        Assert.Equal("1", offers[0].RawMetadata["advertiserId"]);
    }

    [Fact]
    public void Map_ShouldInclude_SectorAndRegion_FromProgramme()
    {
        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1) },
            new[] { MakeProgramme(1) }).ToList();

        Assert.Equal("Electronics", offers[0].RawMetadata["sector"]);
        Assert.Equal("United Kingdom", offers[0].RawMetadata["region"]);
    }

    [Fact]
    public void Map_ShouldInclude_PromoCode_WhenPresent()
    {
        var promo = MakePromotion(advertiserId: 1);
        promo.Code = "SAVE10";

        var offers = AwinOfferMapper.Map(new[] { promo }, new[] { MakeProgramme(1) }).ToList();

        Assert.True(offers[0].RawMetadata.ContainsKey("promoCode"));
        Assert.Equal("SAVE10", offers[0].RawMetadata["promoCode"]);
    }

    // ── Domain Extraction ────────────────────────────────────────────────────

    [Theory]
    [InlineData("https://www.amazon.co.uk", "amazon.co.uk")]
    [InlineData("https://amazon.co.uk", "amazon.co.uk")]
    [InlineData("http://tesco.com", "tesco.com")]
    [InlineData("asos.com", "asos.com")]
    public void Map_ShouldExtract_RetailerDomain_Correctly(string url, string expectedDomain)
    {
        var prog = MakeProgramme(1);
        prog.DisplayUrl = url;

        var offers = AwinOfferMapper.Map(
            new[] { MakePromotion(advertiserId: 1) },
            new[] { prog }).ToList();

        Assert.Equal(expectedDomain, offers[0].RetailerDomain);
    }

    // ── Multiple Offers ──────────────────────────────────────────────────────

    [Fact]
    public void Map_ShouldHandle_MultiplePromotions_AcrossMultipleProgrammes()
    {
        var programmes = new[] { MakeProgramme(1, "Amazon"), MakeProgramme(2, "Tesco") };
        var promo1 = MakePromotion(advertiserId: 1, description: "5% at Amazon");
        promo1.Id = 1;
        var promo2 = MakePromotion(advertiserId: 2, description: "3% at Tesco");
        promo2.Id = 2;

        var offers = AwinOfferMapper.Map(new[] { promo1, promo2 }, programmes).ToList();

        Assert.Equal(2, offers.Count);
        Assert.Contains(offers, o => o.RetailerName == "Amazon");
        Assert.Contains(offers, o => o.RetailerName == "Tesco");
    }

    // ── Commission Resolution Priority ──────────────────────────────────────

    [Fact]
    public void Map_ShouldPrefer_CashbackGroup_OverDefaultGroup()
    {
        var prog = MakeProgramme(1);
        var promo = MakePromotion(advertiserId: 1);
        promo.CommissionGroups = new List<AwinCommissionGroup>
        {
            new() { Name = "default", Type = "percentage", Percentage = 2.0m },
            new() { Name = "cashback", Type = "percentage", Percentage = 8.0m }
        };

        var offers = AwinOfferMapper.Map(new[] { promo }, new[] { prog }).ToList();

        Assert.Equal(8.0m, offers[0].Value);
    }
}
