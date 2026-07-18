using SaverSearch.Application.Common.Interfaces;

namespace SaverSearch.Infrastructure.Services.Caching;

public class RedisCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // Future-proof: Redis cache integration
        return Task.FromResult<T?>(default);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
