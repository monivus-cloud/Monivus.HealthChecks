using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Represents the aggregated health check report for the application.
    /// Contains the overall status, timing information, trace identifier and individual health check entries.
    /// </summary>
    public class HealthCheckReport
    {
        /// <summary>
        /// The overall aggregated health status.
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// The timestamp when the report was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The total duration taken to produce the health report.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The total duration in milliseconds, rounded to three decimal places.
        /// </summary>
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 3);

        /// <summary>
        /// Optional exception message or serialized exception information if the overall health check process encountered an error.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Optional trace identifier associated with this health check execution.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// A dictionary of individual health check entries keyed by their registration name.
        /// </summary>
        public Dictionary<string, HealthCheckEntry> Entries { get; set; } = new Dictionary<string, HealthCheckEntry>();
    }

    /// <summary>
    /// Represents the result of a single health check entry.
    /// Contains status, description, timing, optional data, exception information and tags.
    /// </summary>
    public class HealthCheckEntry
    {
        /// <summary>
        /// The health status of this entry.
        /// </summary>
        public HealthStatus Status { get; set; }

        /// <summary>
        /// Optional textual description provided by the health check.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The duration taken by this individual health check.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The duration in milliseconds for this entry, rounded to three decimal places.
        /// </summary>
        public double DurationMs => Math.Round(Duration.TotalMilliseconds, 3);

        /// <summary>
        /// Optional additional data produced by the health check.
        /// </summary>
        public Dictionary<string, object>? Data { get; set; }

        /// <summary>
        /// Optional exception message or serialized exception information if the health check threw.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Tags associated with this health check entry.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// A string describing the type or category of entry (e.g., "Database", "Dependency").
        /// Defaults to "Unknown" when not set.
        /// </summary>
        public string EntryType { get; set; } = "Unknown";
    }
}
