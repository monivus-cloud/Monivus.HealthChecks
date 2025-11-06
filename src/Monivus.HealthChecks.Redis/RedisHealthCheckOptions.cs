namespace Monivus.HealthChecks.Redis
{
    /// <summary>
    /// Provides configuration options for the Redis health check, allowing customization of
    /// </summary>
    public class RedisHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the Redis connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the threshold in milliseconds for considering a Redis ping as slow.
        /// </summary>
        public double? SlowPingThresholdMilliseconds { get; set; }
    }
}