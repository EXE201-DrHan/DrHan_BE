using Serilog.Core;
using Serilog.Events;

namespace DrHan.API.Extensions
{
    public class VietnamTimeEnricher : ILogEventEnricher
    {
        private static readonly TimeZoneInfo VietnamTimeZone = GetVietnamTimeZone();

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Convert UTC time to Vietnam time (UTC+7)
            var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(logEvent.Timestamp.UtcDateTime, VietnamTimeZone);
            
            var vietnamTimeProperty = propertyFactory.CreateProperty("VietnamTime", vietnamTime);
            logEvent.AddPropertyIfAbsent(vietnamTimeProperty);
        }

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                // Try Windows time zone ID first
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    // Try IANA time zone ID (Linux/macOS)
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback: create a custom time zone for UTC+7
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Vietnam Standard Time",
                        TimeSpan.FromHours(7),
                        "Vietnam Standard Time",
                        "Vietnam Standard Time"
                    );
                }
            }
        }
    }
} 