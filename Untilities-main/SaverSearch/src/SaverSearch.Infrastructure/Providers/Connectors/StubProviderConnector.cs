using SaverSearch.Application.Common.Interfaces.Acquisition;
using SaverSearch.Application.Common.Models.Acquisition;

namespace SaverSearch.Infrastructure.Providers.Connectors;

/// <summary>
/// An in-memory, deterministic provider connector for testing and development.
/// Returns a fixed set of offers. No external calls are made.
/// </summary>
public class StubProviderConnector : IProviderConnector
{
    public string ProviderName => "Stub";
    public string ConnectorVersion => "1.0.0";
    public bool SupportsIncrementalImport => false;

    public Task<IEnumerable<RawProviderOffer>> GetOffersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var offers = new List<RawProviderOffer>
        {
            new(
                ExternalId: "stub-001",
                RetailerName: "Amazon",
                RetailerUrl: "https://www.amazon.co.uk",
                RetailerDomain: "amazon.co.uk",
                Title: "5% Cashback at Amazon",
                Description: "Earn 5% cashback on all purchases.",
                Terms: "Min spend £10. Max reward £50.",
                OfferUrl: "https://stub.example.com/offers/stub-001",
                ValueType: "percentage",
                Value: 5.0m,
                MinimumSpend: 10.0m,
                MaximumReward: 50.0m,
                StartDate: DateTime.UtcNow.AddDays(-7),
                EndDate: DateTime.UtcNow.AddDays(30),
                IsExclusive: false,
                RetrievedAt: now,
                RawMetadata: new Dictionary<string, string> { ["source"] = "stub" }
            ),
            new(
                ExternalId: "stub-002",
                RetailerName: "Tesco",
                RetailerUrl: "https://www.tesco.com",
                RetailerDomain: "tesco.com",
                Title: "£3 off groceries at Tesco",
                Description: "Get £3 off your weekly shop.",
                Terms: "Min spend £25.",
                OfferUrl: "https://stub.example.com/offers/stub-002",
                ValueType: "fixed",
                Value: 3.0m,
                MinimumSpend: 25.0m,
                MaximumReward: null,
                StartDate: DateTime.UtcNow.AddDays(-1),
                EndDate: DateTime.UtcNow.AddDays(14),
                IsExclusive: false,
                RetrievedAt: now,
                RawMetadata: new Dictionary<string, string> { ["source"] = "stub" }
            ),
            new(
                ExternalId: "stub-003",
                RetailerName: "Boots",
                RetailerUrl: "https://www.boots.com",
                RetailerDomain: "boots.com",
                Title: "500 bonus points at Boots",
                Description: "Earn 500 extra Advantage Card points.",
                Terms: null,
                OfferUrl: "https://stub.example.com/offers/stub-003",
                ValueType: "points",
                Value: 500.0m,
                MinimumSpend: 15.0m,
                MaximumReward: null,
                StartDate: null,
                EndDate: DateTime.UtcNow.AddDays(21),
                IsExclusive: true,
                RetrievedAt: now,
                RawMetadata: new Dictionary<string, string> { ["source"] = "stub" }
            ),
            new(
                ExternalId: "stub-004",
                RetailerName: "M&S",
                RetailerUrl: "https://www.marksandspencer.com",
                RetailerDomain: "marksandspencer.com",
                Title: "10% cashback at M&S",
                Description: null,
                Terms: "Clothing only.",
                OfferUrl: "https://stub.example.com/offers/stub-004",
                ValueType: "percentage",
                Value: 10.0m,
                MinimumSpend: null,
                MaximumReward: 25.0m,
                StartDate: DateTime.UtcNow,
                EndDate: DateTime.UtcNow.AddDays(7),
                IsExclusive: false,
                RetrievedAt: now,
                RawMetadata: new Dictionary<string, string> { ["source"] = "stub" }
            ),
            new(
                ExternalId: "stub-005",
                RetailerName: "ASOS",
                RetailerUrl: "https://www.asos.com",
                RetailerDomain: "asos.com",
                Title: "8% cashback at ASOS",
                Description: "Cashback on full-price items.",
                Terms: "Excludes sale items.",
                OfferUrl: "https://stub.example.com/offers/stub-005",
                ValueType: "percentage",
                Value: 8.0m,
                MinimumSpend: null,
                MaximumReward: null,
                StartDate: DateTime.UtcNow.AddDays(-14),
                EndDate: null,
                IsExclusive: false,
                RetrievedAt: now,
                RawMetadata: new Dictionary<string, string> { ["source"] = "stub" }
            )
        };

        return Task.FromResult<IEnumerable<RawProviderOffer>>(offers);
    }

    public Task<ConnectorHealthResult> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConnectorHealthResult(
            IsHealthy: true,
            ProviderName: ProviderName,
            ConnectorVersion: ConnectorVersion,
            CheckedAt: DateTimeOffset.UtcNow,
            LatencyMs: 0,
            Message: "Stub connector is always healthy."
        ));
    }
}
