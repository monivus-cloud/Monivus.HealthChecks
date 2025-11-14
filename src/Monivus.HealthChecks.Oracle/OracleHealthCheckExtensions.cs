using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.Oracle;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering Oracle health checks with the health checks system.
    /// </summary>
    public static class OracleHealthCheckExtensions
    {
        /// <summary>
        /// Adds an Oracle health check registration to the provided <see cref="IHealthChecksBuilder"/>.
        /// Binds <see cref="OracleHealthCheckOptions"/> from configuration path "Monivus:Oracle"
        /// and builds an <see cref="OracleHealthCheck"/> using the resolved connection string.
        /// </summary>
        /// <param name="builder">The health checks builder to add the registration to.</param>
        /// <param name="name">The name of the health check. Defaults to "Oracle".</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the health check fails.</param>
        /// <param name="tags">Optional tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance so that additional calls can be chained.</returns>
        public static IHealthChecksBuilder AddOracleEntry(
            this IHealthChecksBuilder builder,
            string name = "Oracle",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<OracleHealthCheckOptions>()
                .BindConfiguration("Monivus:Oracle");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<OracleHealthCheckOptions>>()?.Value ?? new OracleHealthCheckOptions();

                    ArgumentNullException.ThrowIfNull(opts.ConnectionStringOrName, "ConnectionStringOrName must be provided in configuration or options");

                    var connectionString = ResolveConnectionString(sp, opts.ConnectionStringOrName);

                    return new OracleHealthCheck(opts, connectionString);
                },
                failureStatus,
                PrependTypeTag("Oracle", tags),
                timeout));
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code;
            if (tags is null) yield break;
            foreach (var tag in tags)
            {
                yield return tag;
            }
        }

        private static string ResolveConnectionString(IServiceProvider sp, string connectionStringOrName)
        {
            var configuration = sp.GetService<IConfiguration>();

            if (configuration != null)
            {
                var byName = configuration.GetConnectionString(connectionStringOrName);
                if (!string.IsNullOrWhiteSpace(byName))
                {
                    return byName!;
                }
            }

            return connectionStringOrName;
        }
    }
}

