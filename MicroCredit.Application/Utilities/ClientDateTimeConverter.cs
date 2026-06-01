namespace MicroCredit.Application.Utilities;

public static class ClientDateTimeConverter
{
    public static DateTime NormalizeForStorage(DateTime value, string? timeZoneId)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        // Keep existing behavior for date-only values from <input type="date">.
        // This avoids shifting calendar dates and prevents breaking current flows.
        if (value.TimeOfDay == TimeSpan.Zero)
            return value;

        var tz = ResolveTimeZone(timeZoneId);
        var unspecified = value.Kind == DateTimeKind.Local
            ? DateTime.SpecifyKind(value, DateTimeKind.Unspecified)
            : value;

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }

    public static DateTime GetCurrentDateInTimeZone(string? timeZoneId)
    {
        var tz = ResolveTimeZone(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            var trimmed = timeZoneId.Trim();
            if (TryFindTimeZone(trimmed, out var tz))
                return tz;

            if ((trimmed.Equals("Asia/Kolkata", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.Equals("Asia/Calcutta", StringComparison.OrdinalIgnoreCase)) &&
                TryFindTimeZone("India Standard Time", out var ist))
                return ist;
        }

        return TimeZoneInfo.Utc;
    }

    private static bool TryFindTimeZone(string id, out TimeZoneInfo zone)
    {
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch
        {
            zone = TimeZoneInfo.Utc;
            return false;
        }
    }
}
