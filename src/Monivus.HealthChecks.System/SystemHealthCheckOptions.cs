namespace Monivus.HealthChecks.System
{
    /// <summary>
    /// Provides configuration options for the System health check, allowing customization of
    /// </summary>
    public class SystemHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the memory usage threshold percentage.
        /// </summary>
        public double? MemoryUsageThresholdPercent { get; set; }
    }
}
