namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheckOptions
    {
        public int MinServers { get; set; } = 1;
        public int MaxFailedJobs { get; set; } = 0;
        public int MaxEnqueuedJobs { get; set; } = 0;
    }
}
