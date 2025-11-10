using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.PostgreSql;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering PostgreSQL health checks with the health checks system.
    /// </summary>
    public static class PostgreSqlHealthCheckExtensions
    {
        /// <summary>
        /// Adds a PostgreSQL health check registration to the provided <see cref="IHealthChecksBuilder"/>.
        /// The method binds <see cref="PostgreSqlHealthCheckOptions"/> from configuration path "Monivus:PostgreSql"
        /// and creates a <see cref="PostgreSqlHealthCheck"/> using the resolved connection string.
        /// </summary>
        /// <param name="builder">The health checks builder to add the registration to.</param>
        /// <param name="name">The name of the health check. Defaults to "PostgreSql".</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the health check fails.</param>
        /// <param name="tags">Optional tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance so that additional calls can be chained.</returns>
        public static IHealthChecksBuilder AddPostgreSqlEntry(
            this IHealthChecksBuilder builder,
            string name = "PostgreSql",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<PostgreSqlHealthCheckOptions>()
                .BindConfiguration($"Monivus:PostgreSql");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<PostgreSqlHealthCheckOptions>>()?.Value ?? new PostgreSqlHealthCheckOptions();

                    ArgumentNullException.ThrowIfNull(opts.ConnectionStringOrName, "ConnectionName must be provided in configuration or options");

                    var connectionString = ResolveConnectionString(sp, opts.ConnectionStringOrName);

                    return new PostgreSqlHealthCheck(opts, connectionString);
                },
                failureStatus,
                PrependTypeTag("PostgreSql", tags),
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

