using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Infrastructure.Providers.Scrapers;

public class TopCashbackScraper : IOfferScraper
{
    public string ProviderName => "TopCashback";

    public Task<IEnumerable<Offer>> ScrapeOffersAsync(Retailer retailer, CancellationToken cancellationToken = default)
    {
        // Future-proof: Logic for scraping TopCashback using Playwright / HttpClient / HtmlAgilityPack
        return Task.FromResult<IEnumerable<Offer>>(new List<Offer>());
    }
}
