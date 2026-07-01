namespace SaverSearch.Application.Common.Interfaces;

public interface IExternalApiClient<TResponse>
{
    Task<TResponse?> GetOffersAsync(string query, CancellationToken cancellationToken = default);
}
