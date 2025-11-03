using Hangfire.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.Hangfire
{
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly IMonitoringApi _monitoringApi;
        private readonly HangfireHealthCheckOptions _options;

        public HangfireHealthCheck(IMonitoringApi monitoringApi, HangfireHealthCheckOptions? options = null)
        {
            _monitoringApi = monitoringApi ?? throw new ArgumentNullException(nameof(monitoringApi));
            _options = options ?? new HangfireHealthCheckOptions();
        }

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

                if (_options.MaxFailedJobs > 0 && stats.Failed > _options.MaxFailedJobs)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {stats.Failed} failed job(s), expected max {_options.MaxFailedJobs}."));
                }

                if (_options.MaxEnqueuedJobs > 0 && stats.Enqueued > _options.MaxEnqueuedJobs)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire is running but has {stats.Enqueued} enqueued job(s), expected max {_options.MaxEnqueuedJobs}."));
                }

                var now = DateTime.UtcNow;
                var serverCount = servers.Count;

                if (serverCount < _options.MinServers)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Hangfire has {serverCount} registered servers, but at least {_options.MinServers} are expected."));
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
