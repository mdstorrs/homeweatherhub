using System;

namespace api.Model
{
    // The time used for DateAdded, LastActive and ServerTime.
    // All existing data is in Brisbane time, so the API must not rely on the database
    // server's clock: a new host can be in another time zone (SmartASP runs on US Pacific time).
    public static class AppTime
    {
        private static TimeZoneInfo zone = Resolve("Australia/Brisbane");

        // Set once at startup from configuration (AppTimeZone); IANA or Windows ids both work.
        public static void Configure(string? timeZoneId)
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                zone = Resolve(timeZoneId);
            }
        }

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);

        private static TimeZoneInfo Resolve(string id)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
            {
                return tz;
            }

            // Windows servers without ICU only know Windows ids (e.g. "E. Australia Standard Time").
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windowsId) &&
                TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out tz))
            {
                return tz;
            }

            throw new InvalidOperationException($"Unknown time zone '{id}'. Check AppTimeZone in appsettings.json.");
        }
    }
}
