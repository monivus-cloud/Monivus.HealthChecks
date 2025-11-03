using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Monivus.HealthChecks.Hangfire
{
    public static class HangfireHealthCheckExtensions
    {
        /// <summary>
        /// Adds a health check for Hangfire background job processing system
        /// </summary>
        /// <param name="builder">The health checks builder</param>
        /// <param name="name">The health check name (optional)</param>
        /// <param name="failureStatus">The failure status (optional)</param>
        /// <param name="tags">Additional tags (optional)</param>
        /// <param name="timeout">The health check timeout (optional)</param>
        /// <returns>The health checks builder</returns>
        public static IHealthChecksBuilder AddHangfireEntry(
            this IHealthChecksBuilder builder,
            string name = "hangfire",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.Add(new HealthCheckRegistration(
                name,
                serviceProvider =>
                {
                    try
                    {
                        var jobStorage = serviceProvider.GetService<JobStorage>()
                            ?? JobStorage.Current;

                        if (jobStorage == null)
                        {
                            throw new InvalidOperationException("Hangfire JobStorage is not configured");
                        }

                        var monitoringApi = jobStorage.GetMonitoringApi();
                        var opts = serviceProvider.GetService<IOptions<HangfireHealthCheckOptions>>()?.Value
                                   ?? new HangfireHealthCheckOptions();
                        return new HangfireHealthCheck(monitoringApi, opts);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to create Hangfire health check", ex);
                    }
                },
                failureStatus,
                PrependTypeTag("Hangfire", tags),
                timeout));
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code.ToString();
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
        }
    }
}
