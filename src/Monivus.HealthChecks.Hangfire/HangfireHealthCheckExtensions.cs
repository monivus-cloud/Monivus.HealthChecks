using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.Hangfire;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering Hangfire-based health checks with the health checks system.
    /// </summary>
    /// <remarks>This class contains methods to integrate Hangfire monitoring with the health checks
    /// infrastructure. It allows the registration of a health check that monitors the status of Hangfire's <see
    /// cref="JobStorage"/>  and its associated monitoring API.</remarks>
    public static class HangfireHealthCheckExtensions
    {
        /// <summary>
        /// Registers a Hangfire-based health check with the provided <see cref="IHealthChecksBuilder"/>.
        /// Binds configuration from the "Monivus:Hangfire" section to <see cref="HangfireHealthCheckOptions"/>,
        /// resolves the Hangfire <see cref="JobStorage"/> (or falls back to <see cref="JobStorage.Current"/>),
        /// obtains the monitoring API and constructs a <see cref="HangfireHealthCheck"/> instance.
        /// </summary>
        /// <param name="builder">The health checks builder to which the Hangfire check will be added.</param>
        /// <param name="name">The name of the health check registration. Defaults to "Hangfire".</param>
        /// <param name="failureStatus">
        /// Optional <see cref="HealthStatus"/> to report when the check fails. If null, the default failure status is used.
        /// </param>
        /// <param name="tags">Optional tags to associate with the health check registration.</param>
        /// <param name="timeout">Optional timeout for the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance to allow chaining.</returns>
        /// <remarks>
        /// Throws <see cref="InvalidOperationException"/> if the Hangfire <see cref="JobStorage"/> cannot be resolved
        /// or if constructing the health check fails.
        /// </remarks>
        public static IHealthChecksBuilder AddHangfireEntry(
            this IHealthChecksBuilder builder,
            string name = "Hangfire",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<HangfireHealthCheckOptions>()
                .BindConfiguration($"Monivus:Hangfire");

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
