using System.Diagnostics;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SaverSearch.Application.Common.Interfaces;
using SaverSearch.Application.Common.Models.Pipeline;
using SaverSearch.Application.Dtos;
using SaverSearch.Domain.Entities;

namespace SaverSearch.Application.Services.Pipeline;

public class OfferResolver(
    IUnitOfWork unitOfWork,
    IMapper mapper) : IOfferResolver
{
    public async Task<OfferResolverResponse> ResolveOffersAsync(
        DiscoveryContext context,
        RetailerDto retailer,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var query = unitOfWork.Offers.GetQueryable(asNoTracking: true)
            .Include(o => o.Retailer)
            .Include(o => o.Provider)
            .Include(o => o.OfferType)
            .Where(o => o.RetailerId == retailer.Id);

        List<Offer> rawOffers;
        if (query is IAsyncEnumerable<Offer>)
        {
            rawOffers = await query.ToListAsync(cancellationToken);
        }
        else
        {
            rawOffers = query.ToList();
        }

        var examined = rawOffers.Count;
        var rejected = 0;
        var rejectionReasons = new Dictionary<Guid, string>();
        var resolvedOffers = new List<ResolvedOffer>();
        var now = DateTime.UtcNow;

        foreach (var offer in rawOffers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // 1. IsActive Check
            if (!offer.IsActive)
            {
                rejected++;
                rejectionReasons[offer.Id] = "Offer is disabled (IsActive == false).";
                continue;
            }

            // 2. StartDate Check
            if (offer.StartDate.HasValue && offer.StartDate.Value > now)
            {
                rejected++;
                rejectionReasons[offer.Id] = $"Offer is not yet active (StartDate: {offer.StartDate.Value}).";
                continue;
            }

            // 3. EndDate Check
            if (offer.EndDate.HasValue && offer.EndDate.Value < now)
            {
                rejected++;
                rejectionReasons[offer.Id] = $"Offer is expired (EndDate: {offer.EndDate.Value}).";
                continue;
            }

            // 4. Retailer Active Check
            if (offer.Retailer == null || !offer.Retailer.IsActive)
            {
                rejected++;
                rejectionReasons[offer.Id] = "Associated retailer is inactive or missing.";
                continue;
            }

            // 5. Provider Active Check
            if (offer.Provider == null || !offer.Provider.IsActive)
            {
                rejected++;
                rejectionReasons[offer.Id] = "Associated provider is inactive or missing.";
                continue;
            }

            // Projection to DTOs
            var offerDto = mapper.Map<OfferDto>(offer);
            var retailerDto = mapper.Map<RetailerDto>(offer.Retailer);
            var providerDto = mapper.Map<ProviderDto>(offer.Provider);
            var offerTypeDto = mapper.Map<OfferTypeDto>(offer.OfferType);

            resolvedOffers.Add(new ResolvedOffer(
                offerDto,
                retailerDto,
                providerDto,
                offerTypeDto,
                OfferSource.Database,
                now
            ));
        }

        stopwatch.Stop();

        var diagnostics = new OfferResolverDiagnostics
        {
            RetrievalDurationMs = stopwatch.ElapsedMilliseconds,
            OffersExamined = examined,
            OffersReturned = resolvedOffers.Count,
            OffersRejected = rejected,
            RejectionReasons = rejectionReasons
        };

        return new OfferResolverResponse(resolvedOffers, diagnostics);
    }
}
