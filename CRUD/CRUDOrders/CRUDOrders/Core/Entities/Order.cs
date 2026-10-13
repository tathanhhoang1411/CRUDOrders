namespace CRUDOrders.Core.Entities;

public class Order : Entity
{
    public long UserId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled
    public decimal Total { get; set; }
    public string? Note { get; set; }

    public virtual List<OrderItem> OrderItems { get; set; } = new();
}
