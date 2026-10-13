using StackExchange.Redis;
using System.Text.Json;

namespace CRUDOrders.Infrastructure.Cache;

/// <summary>
/// Trien khai ICacheService dung Redis (StackExchange.Redis).
/// Duoc dang ky khi Features:EnableRedisCache = true.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly IServer _server;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints()[0]);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _db.StringSetAsync(key, json, expiry ?? DefaultTtl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var keys = _server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Length > 0)
            await _db.KeyDeleteAsync(keys);
    }
}
