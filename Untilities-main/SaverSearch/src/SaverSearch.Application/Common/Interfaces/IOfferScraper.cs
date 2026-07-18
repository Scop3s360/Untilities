using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Common.Interfaces;

public interface IOfferScraper
{
    string ProviderName { get; }
    Task<IEnumerable<Offer>> ScrapeOffersAsync(Retailer retailer, CancellationToken cancellationToken = default);
}
