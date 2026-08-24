namespace UniRemoteExam.Services;

public static class YemenTime
{
    private static readonly Lazy<TimeZoneInfo> YemenZone = new(() =>
    {
        foreach (var id in new[] { "Asia/Aden", "Arab Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Yemen", TimeSpan.FromHours(3), "Yemen", "Yemen");
    });

    public static DateTime? LocalInputToUtc(DateTime? local)
    {
        if (!local.HasValue) return null;
        var unspecified = DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, YemenZone.Value);
    }

    public static DateTime UtcNow => DateTime.UtcNow;

    public static DateTime ToLocal(DateTime utc)
    {
        var normalized = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(normalized, YemenZone.Value);
    }

    public static DateTime? ToLocal(DateTime? utc) => utc.HasValue ? ToLocal(utc.Value) : null;
}
