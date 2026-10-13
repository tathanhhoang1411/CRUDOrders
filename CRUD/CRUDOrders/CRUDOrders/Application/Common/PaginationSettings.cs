namespace CRUDOrders.Application.Common;

/// <summary>
/// Cau hinh phan trang duoc doc tu appsettings.json (section "Pagination").
/// </summary>
public class PaginationSettings
{
    public const string SectionName = "Pagination";

    /// <summary>Gia tri skip toi da (mac dinh: 99, nghia la be hon 100).</summary>
    public int MaxSkip { get; set; } = 99;

    /// <summary>Gia tri skip toi thieu (mac dinh: 0, khong duoc be hon 0).</summary>
    public int MinSkip { get; set; } = 0;

    /// <summary>Gia tri take toi thieu (mac dinh: 1).</summary>
    public int MinTake { get; set; } = 1;

    /// <summary>Gia tri take toi da (mac dinh: 100).</summary>
    public int MaxTake { get; set; } = 100;
}
