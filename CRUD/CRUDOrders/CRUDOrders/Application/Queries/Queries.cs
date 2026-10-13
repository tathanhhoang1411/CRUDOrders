using MediatR;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Behaviors;
using System.Text.Json;

namespace CRUDOrders.Application.Queries;

public class GetProductByIdQuery : IRequest<Result<ProductDto>>, ICacheable
{
    public long Id { get; set; }
    public string CacheKey => $"product:id:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public class GetProductsQuery : IRequest<Result<PagedResult<ProductDto>>>, ICacheable
{
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
    public string? Keyword { get; set; }
    public CancellationToken CancellationToken { get; set; }

    public string CacheKey => $"products:skip={Skip}:take={Take}:kw={Keyword ?? ""}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class GetOrderByIdQuery : IRequest<Result<OrderDto>>, ICacheable
{
    public long Id { get; set; }
    public string CacheKey => $"order:id:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public class GetOrdersQuery : IRequest<Result<PagedResult<OrderDto>>>
{
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
    public string? Keyword { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class GetUserOrdersQuery : IRequest<Result<PagedResult<OrderDto>>>
{
    public long UserId { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 10;
    public CancellationToken CancellationToken { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
}
