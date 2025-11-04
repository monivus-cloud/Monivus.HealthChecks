namespace Monivus.HealthChecks.Redis
{
    public class RedisHealthCheckOptions
    {
        public string? ConnectionString { get; set; }
        public double? SlowPingThresholdMilliseconds { get; set; }
    }
}