using Hangfire.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.Hangfire
{
    /// <summary>
    /// Provides a health check for monitoring the status of a Hangfire storage and its associated servers.
    /// </summary>
    /// <remarks>This health check evaluates the accessibility of the Hangfire storage, the number of
    /// registered servers, and various job statistics such as the number of failed, enqueued, and processing jobs. It
    /// can be configured to enforce thresholds for these metrics using <see
    /// cref="HangfireHealthCheckOptions"/>.</remarks>
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly IMonitoringApi _monitoringApi;
        private readonly HangfireHealthCheckOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="HangfireHealthCheck"/> class.
        /// </summary>
        /// <param name="monitoringApi">The Hangfire monitoring API used to retrieve job and server statistics. Cannot be <see langword="null"/>.</param>
        /// <param name="options">The configuration options for the health check. Can be <see langword="null"/> to use default settings.</param>
        public HangfireHealthCheck(IMonitoringApi monitoringApi, HangfireHealthCheckOptions options)
        {
            ArgumentNullException.ThrowIfNull(monitoringApi);

            _monitoringApi = monitoringApi;
            _options = options;
        }

        /// <summary>
        /// Performs a health check on the Hangfire storage and its associated servers.
        /// </summary>
        /// <remarks>This method evaluates the health of the Hangfire storage by checking the availability
        /// of servers, connection statistics, and job metrics. It returns a health status of <see
        /// cref="HealthStatus.Healthy"/>, <see cref="HealthStatus.Degraded"/>, or <see cref="HealthStatus.Unhealthy"/>
        /// based on the following conditions: <list type="bullet"> <item> <description> If the Hangfire storage or its
        /// connection is inaccessible, the result is <see cref="HealthStatus.Unhealthy"/>. </description> </item>
        /// <item> <description> If the number of failed jobs exceeds the configured maximum, or the number of enqueued
        /// jobs exceeds the configured maximum, or the number of registered servers is below the configured minimum,
        /// the result is <see cref="HealthStatus.Degraded"/>. </description> </item> <item> <description> If all checks
        /// pass, the result is <see cref="HealthStatus.Healthy"/>. Additional health data, such as job statistics and
        /// server information, is included in the result. </description> </item> </list> Exceptions encountered during
        /// the health check are captured and included in the result as part of the <see
        /// cref="HealthCheckResult.Unhealthy"/> status.</remarks>
        /// <param name="context">The <see cref="HealthCheckContext"/> that provides context for the health check operation.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the health check operation.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains a <see
        /// cref="HealthCheckResult"/> indicating the health status of the Hangfire storage and servers.</returns>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var servers = _monitoringApi.Servers();
                if (servers == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not accessible"));
                }

                var stats = _monitoringApi.GetStatistics();
                if (stats == null)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage connection failed"));
                }

                if (_options.MaxFailedJobs.HasValue && stats.Failed > _options.MaxFailedJobs.Value)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {stats.Failed} failed job(s), expected max {_options.MaxFailedJobs.Value}."));
                }

                if (_options.MaxEnqueuedJobs.HasValue && stats.Enqueued > _options.MaxEnqueuedJobs.Value)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {stats.Enqueued} enqueued job(s), expected max {_options.MaxEnqueuedJobs.Value}."));
                }

                var serverCount = servers.Count;

                if (_options.MinServers.HasValue && serverCount < _options.MinServers.Value)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire has {serverCount} registered servers, but at least {_options.MinServers.Value} are expected."));
                }

                var lastHeartbeat = servers
                    .Where(s => s.Heartbeat.HasValue)
                    .Select(s => s.Heartbeat!.Value)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                var healthData = new Dictionary<string, object>
                {
                    ["TotalServers"] = serverCount,
                    ["SucceededJobs"] = stats.Succeeded,
                    ["FailedJobs"] = stats.Failed,
                    ["ProcessingJobs"] = stats.Processing,
                    ["ScheduledJobs"] = stats.Scheduled,
                    ["EnqueuedJobs"] = stats.Enqueued,
                    ["DeletedJobs"] = stats.Deleted,
                    ["RecurringJobs"] = stats.Recurring,
                };

                if (lastHeartbeat != DateTime.MinValue)
                {
                    healthData["LastServerHeartbeat"] = lastHeartbeat.ToString("o");
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    "Hangfire is healthy and running",
                    healthData));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Hangfire health check failed",
                    ex,
                    new Dictionary<string, object> { ["Error"] = ex.Message }));
            }
        }
    }
}
