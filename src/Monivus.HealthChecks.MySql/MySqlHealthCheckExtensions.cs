using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.MySql;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering MySQL health checks with the health checks system.
    /// </summary>
    public static class MySqlHealthCheckExtensions
    {
        /// <summary>
        /// Adds a MySQL health check registration to the provided <see cref="IHealthChecksBuilder"/>.
        /// The method binds <see cref="MySqlHealthCheckOptions"/> from configuration path "Monivus:MySql"
        /// and creates a <see cref="MySqlHealthCheck"/> using the resolved connection string.
        /// </summary>
        /// <param name="builder">The health checks builder to add the registration to.</param>
        /// <param name="name">The name of the health check. Defaults to "MySql".</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the health check fails.</param>
        /// <param name="tags">Optional tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance so that additional calls can be chained.</returns>
        public static IHealthChecksBuilder AddMySqlEntry(
            this IHealthChecksBuilder builder,
            string name = "MySql",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<MySqlHealthCheckOptions>()
                .BindConfiguration($"Monivus:MySql");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<MySqlHealthCheckOptions>>()?.Value ?? new MySqlHealthCheckOptions();

                    ArgumentNullException.ThrowIfNull(opts.ConnectionStringOrName, "ConnectionName must be provided in configuration or options");

                    var connectionString = ResolveConnectionString(sp, opts.ConnectionStringOrName);

                    return new MySqlHealthCheck(opts, connectionString);
                },
                failureStatus,
                PrependTypeTag("MySql", tags),
                timeout));
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code.ToString();
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
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
