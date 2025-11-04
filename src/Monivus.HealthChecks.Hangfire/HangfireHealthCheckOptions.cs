namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheckOptions
    {
        public int? MinServers { get; set; }
        public int? MaxFailedJobs { get; set; }
        public int? MaxEnqueuedJobs { get; set; }
    }
}
