namespace Monivus.HealthChecks.Redis
{
    public class RedisHealthCheckOptions
    {
        public double SlowPingThresholdMilliseconds { get; set; } = 1000;
    }
}