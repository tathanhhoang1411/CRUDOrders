using MediatR;
using CRUDOrders.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior tu dong cache ket qua cua cac Query implement ICacheable.
/// Neu co cache hop le, tra ve ngay tu cache ma khong goi DB.
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await next();

        if (!cacheable.ForceRefresh)
        {
            var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("[Cache HIT] {Key}", cacheable.CacheKey);
                return cached;
            }
        }

        _logger.LogDebug("[Cache MISS] {Key}", cacheable.CacheKey);
        var response = await next();

        if (response is not null)
            await _cache.SetAsync(cacheable.CacheKey, response, cacheable.CacheDuration, cancellationToken);

        return response;
    }
}
