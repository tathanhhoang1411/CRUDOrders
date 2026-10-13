namespace CRUDOrders.Application.Behaviors;

/// <summary>
/// Implement interface nay tren cac Query muon duoc tu dong cache boi CachingBehavior.
/// </summary>
public interface ICacheable
{
    /// <summary>Cache key duy nhat cho request nay.</summary>
    string CacheKey { get; }

    /// <summary>Thoi gian song cua cache. Null = dung TTL mac dinh (5 phut).</summary>
    TimeSpan? CacheDuration => null;

    /// <summary>Neu true, bo qua cache va buoc query DB, sau do cap nhat lai cache.</summary>
    bool ForceRefresh => false;
}
