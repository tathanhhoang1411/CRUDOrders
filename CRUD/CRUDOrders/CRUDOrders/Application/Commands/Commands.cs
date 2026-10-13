using MediatR;
using CRUDOrders.Application.DTOs;

namespace CRUDOrders.Application.Commands;

public class CreateProductCommand : IRequest<Result<ProductDto>>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class UpdateProductCommand : IRequest<Result<ProductDto>>
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class DeleteProductCommand : IRequest<Result<bool>>
{
    public long Id { get; set; }
}

public class CreateOrderCommand : IRequest<Result<OrderDto>>
{
    public long UserId { get; set; }
    public List<OrderItemInput> OrderItems { get; set; } = new();
    public string? Note { get; set; }
}

public class UpdateOrderStatusCommand : IRequest<Result<OrderDto>>
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DeleteOrderCommand : IRequest<Result<bool>>
{
    public long Id { get; set; }
}

public class OrderItemInput
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}
