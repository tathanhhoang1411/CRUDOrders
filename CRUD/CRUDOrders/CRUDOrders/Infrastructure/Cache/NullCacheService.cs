namespace CRUDOrders.Infrastructure.Cache;

/// <summary>
/// No-op implementation cua ICacheService khi Redis bi tat (Features:EnableRedisCache = false).
/// Dam bao application chay binh thuong ma khong can Redis.
/// </summary>
public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(default(T));

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
