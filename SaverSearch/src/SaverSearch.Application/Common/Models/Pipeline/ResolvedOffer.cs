using SaverSearch.Application.Dtos;

namespace SaverSearch.Application.Common.Models.Pipeline;

public record ResolvedOffer(
    OfferDto Offer,
    RetailerDto Retailer,
    ProviderDto Provider,
    OfferTypeDto OfferType,
    OfferSource Source,
    DateTime RetrievedTimestamp
);

public record OfferResolverResponse(
    IEnumerable<ResolvedOffer> Offers,
    OfferResolverDiagnostics Diagnostics
);
