using CRUDOrders.Application.Common;

namespace CRUDOrders.Core.Entities;

/// <summary>
/// Base entity class for all domain entities.
/// </summary>
public abstract class Entity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTimeHelper.UtcPlus7Now;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
