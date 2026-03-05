namespace TelemetrySlice.Lib.Extensions;

public static class DateExtensions
{
    public static long ToUnixTimeStamp(this DateTime date)
    {
        return ((DateTimeOffset)date).ToUnixTimeSeconds();
    }
}