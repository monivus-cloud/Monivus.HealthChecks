namespace Monivus.HealthChecks.Hangfire
{
    /// <summary>
    /// Provides configuration options for the Hangfire health check, allowing customization of thresholds for server
    /// availability, job failures, and enqueued jobs.
    /// </summary>
    /// <remarks>These options are used to define the criteria for determining the health status of a Hangfire
    /// system.  If any of the specified thresholds are exceeded, the health check may report a degraded or unhealthy
    /// status.</remarks>
    public class HangfireHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the minimum number of servers required to handle requests.
        /// </summary>
        public int? MinServers { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of failed jobs allowed before the system takes corrective action.
        /// </summary>
        public int? MaxFailedJobs { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of jobs that can be enqueued at a time.
        /// </summary>
        public int? MaxEnqueuedJobs { get; set; }
    }
}
