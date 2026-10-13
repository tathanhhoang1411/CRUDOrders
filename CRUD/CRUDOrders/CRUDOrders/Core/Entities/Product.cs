namespace CRUDOrders.Core.Entities;

public class Product : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    /// <summary>
    /// Concurrency token (SQL Server ROWVERSION) chong ban vuot ton kho (overselling)
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
