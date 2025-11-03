using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public class HealthCheckReport
    {
        public HealthStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 3);
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, HealthCheckEntry> Entries { get; set; } = [];
    }

    public class HealthCheckEntry
    {
        public HealthStatus Status { get; set; }
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 3);
        public Dictionary<string, object>? Data { get; set; }
        public string? Exception { get; set; }
        public IEnumerable<string> Tags { get; set; } = [];
        public string EntryType { get; set; } = "Unknown";
    }
}
