using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.System;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering System health checks with the health checks system.
    /// </summary>
    public static class SystemHealthCheckExtensions
    {
        /// <summary>
        /// Adds the System health check to the specified <see cref="IHealthChecksBuilder"/>.
        /// </summary>
        /// <param name="builder">The health checks builder to which the System health check will be added. Cannot be null.</param>
        /// <param name="name">The registration name for the health check. Defaults to "System".</param>
        /// <param name="failureStatus">
        /// The health status to report when the check fails. If null, the default failure status for the health check registration is used.
        /// </param>
        /// <param name="tags">Optional tags to associate with this health check registration.</param>
        /// <param name="timeout">An optional timeout that will be applied to the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance for chaining further registrations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
        public static IHealthChecksBuilder AddSystemEntry(
            this IHealthChecksBuilder builder,
            string name = "System",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<SystemHealthCheckOptions>()
                .BindConfiguration($"Monivus:System");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<SystemHealthCheckOptions>>()?.Value ?? new SystemHealthCheckOptions();

                    return new SystemHealthCheck(opts);
                },
                failureStatus,
                PrependTypeTag("System", tags),
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
