using AutoMapper;
using MediatR;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Queries;
using CRUDOrders.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Application.Handlers;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", request.Id);
            return Result<ProductDto>.Error("PRODUCT_NOT_FOUND", "Sản phẩm không tồn tại");
        }

        return Result<ProductDto>.Ok(_mapper.Map<ProductDto>(product));
    }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Min(100, Math.Max(1, request.Take));

        var products = await _productRepository.GetAllAsync(skip, take, request.Keyword, cancellationToken);
        var total = await _productRepository.CountAsync(request.Keyword, cancellationToken);

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = _mapper.Map<List<ProductDto>>(products),
            Total = total,
            Skip = skip,
            Take = take
        };

        return Result<PagedResult<ProductDto>>.Ok(pagedResult);
    }
}

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IOrderRepository orderRepository,
        IMapper mapper,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.Id);
            return Result<OrderDto>.Error("ORDER_NOT_FOUND", "Đơn hàng không tồn tại");
        }

        return Result<OrderDto>.Ok(_mapper.Map<OrderDto>(order));
    }
}

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(
        IOrderRepository orderRepository,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Min(100, Math.Max(1, request.Take));

        var orders = await _orderRepository.GetAllAsync(skip, take, request.Keyword, cancellationToken);
        var total = await _orderRepository.CountAsync(request.Keyword, cancellationToken);

        var pagedResult = new PagedResult<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(orders),
            Total = total,
            Skip = skip,
            Take = take
        };

        return Result<PagedResult<OrderDto>>.Ok(pagedResult);
    }
}

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserOrdersQueryHandler> _logger;

    public GetUserOrdersQueryHandler(
        IOrderRepository orderRepository,
        IMapper mapper,
        ILogger<GetUserOrdersQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, request.Skip);
        var take = Math.Min(100, Math.Max(1, request.Take));

        // Goi thang DB, loc theo UserId ngay tai SQL — khong load all roi filter in-memory
        var (items, total) = await _orderRepository.GetUserOrdersAsync(
            request.UserId, skip, take, cancellationToken);

        _logger.LogDebug("GetUserOrders: userId={UserId}, total={Total}", request.UserId, total);

        var pagedResult = new PagedResult<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(items),
            Total = total,
            Skip = skip,
            Take = take
        };

        return Result<PagedResult<OrderDto>>.Ok(pagedResult);
    }
}
