using AutoMapper;
using MediatR;
using CRUDOrders.Application.Commands;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Application.Common;
using CRUDOrders.Core.Entities;
using CRUDOrders.Infrastructure.Data;
using CRUDOrders.Infrastructure.Data.Repositories;
using CRUDOrders.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly ApplicationDbContext _context;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IIdGenerator idGenerator,
        IMapper mapper,
        ILogger<CreateOrderCommandHandler> logger,
        ApplicationDbContext context)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _idGenerator = idGenerator;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validation da duoc xu ly boi ValidationBehavior trong pipeline
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        }

        try
        {
            var order = new Order
            {
                Id = _idGenerator.NewId(),
                UserId = request.UserId,
                Status = "Pending",
                Note = request.Note,
                CreatedAt = DateTimeHelper.UtcPlus7Now
            };

            decimal totalPrice = 0;

            foreach (var item in request.OrderItems)
            {
                // Lay co tracking de EF Core co the detect DbUpdateConcurrencyException
                var product = await _productRepository.GetByIdTrackedAsync(item.ProductId, cancellationToken);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found", item.ProductId);
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDto>.Error("PRODUCT_NOT_FOUND", $"San pham ID {item.ProductId} khong ton tai");
                }

                if (product.Stock < item.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                        item.ProductId, product.Stock, item.Quantity);
                    if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                    return Result<OrderDto>.Error("INSUFFICIENT_STOCK",
                        $"Ton kho khong du cho san pham '{product.Name}'. Con lai: {product.Stock}");
                }

                var orderItem = new OrderItem
                {
                    Id = _idGenerator.NewId(),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    CreatedAt = DateTimeHelper.UtcPlus7Now
                };

                order.OrderItems.Add(orderItem);
                totalPrice += orderItem.TotalPrice;

                // Tru ton kho — loi DbUpdateConcurrencyException se duoc bat o duoi
                product.Stock -= item.Quantity;
                product.UpdatedAt = DateTimeHelper.UtcPlus7Now;
            }

            order.Total = totalPrice;
            await _context.Orders.AddAsync(order, cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex,
                    "Concurrency conflict khi tao don hang cho user {UserId}. Co the ton kho da thay doi.",
                    request.UserId);
                return Result<OrderDto>.Error("CONCURRENT_CONFLICT",
                    "Du lieu ton kho vua duoc cap nhat boi request khac. Vui long thu lai.");
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Order {OrderId} created successfully with {ItemCount} items",
                order.Id, order.OrderItems.Count);

            var orderDto = _mapper.Map<OrderDto>(order);
            return Result<OrderDto>.Ok(orderDto);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (transaction != null) await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Unexpected error when creating order for user {UserId}", request.UserId);
            return Result<OrderDto>.Error("INTERNAL_ERROR", "Tao don hang that bai, vui long thu lai sau.");
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }
}

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IMapper mapper,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<OrderDto>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.Id);
            return Result<OrderDto>.Error("ORDER_NOT_FOUND", "Don hang khong ton tai");
        }

        var validStatuses = new[] { "Pending", "Completed", "Cancelled" };
        if (!validStatuses.Contains(request.Status))
        {
            _logger.LogWarning("Invalid status {Status} for order {OrderId}", request.Status, request.Id);
            return Result<OrderDto>.Error("INVALID_STATUS", "Trang thai don hang khong hop le. Cac gia tri cho phep: Pending, Completed, Cancelled");
        }

        order.Status = request.Status;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to {Status}", order.Id, request.Status);

        return Result<OrderDto>.Ok(_mapper.Map<OrderDto>(order));
    }
}

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result<bool>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(
        IOrderRepository orderRepository,
        ILogger<DeleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", request.Id);
            return Result<bool>.Error("ORDER_NOT_FOUND", "Don hang khong ton tai");
        }

        await _orderRepository.DeleteAsync(request.Id, cancellationToken);
        _logger.LogInformation("Order {OrderId} deleted successfully", request.Id);

        return Result<bool>.Ok(true);
    }
}
