namespace CRUDOrders.Application.Common;

/// <summary>
/// Cung cap thoi gian chuan UTC+7 (Asia/Ho_Chi_Minh).
/// Dung helper nay thay vi DateTime.UtcNow.AddHours(7) cung de tranh sai lech DST.
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo Vn = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");

    /// <summary>Thoi gian hien tai theo mui gio UTC+7 (Viet Nam).</summary>
    public static DateTime UtcPlus7Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Vn);
}
