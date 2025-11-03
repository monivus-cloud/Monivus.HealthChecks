using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;

namespace Monivus.HealthChecks.SqlServer
{
    public static class SqlServerHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddSqlServerEntry(
            this IHealthChecksBuilder builder,
            string connectionStringOrName,
            string testQuery = "SELECT 1",
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrEmpty(connectionStringOrName))
                throw new ArgumentNullException(nameof(connectionStringOrName));

            return builder.Add(new HealthCheckRegistration(
                name ?? "sqlserver",
                sp =>
                {
                    var resolved = ResolveConnectionString(sp, connectionStringOrName);
                    var options = new SqlServerHealthCheckOptions
                    {
                        ConnectionString = resolved,
                        TestQuery = testQuery
                    };

                    if (timeout.HasValue)
                    {
                        options.Timeout = timeout.Value;
                    }

                    return new SqlServerHealthCheck(options);
                },
                failureStatus,
                PrependTypeTag("SqlServer", tags),
                timeout));
        }

        public static IHealthChecksBuilder AddSqlServerEntry(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, string> connectionStringFactory,
            string testQuery = "SELECT 1",
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (connectionStringFactory == null)
                throw new ArgumentNullException(nameof(connectionStringFactory));

            return builder.Add(new HealthCheckRegistration(
                name ?? "sqlserver",
                sp =>
                {
                    var connectionString = connectionStringFactory(sp);
                    var options = new SqlServerHealthCheckOptions
                    {
                        ConnectionString = connectionString,
                        TestQuery = testQuery
                    };

                    if (timeout.HasValue)
                    {
                        options.Timeout = timeout.Value;
                    }

                    return new SqlServerHealthCheck(options);
                },
                failureStatus,
                PrependTypeTag("SqlServer", tags),
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

            if (LooksLikeConnectionString(connectionStringOrName))
            {
                return connectionStringOrName;
            }

            return connectionStringOrName;
        }

        private static bool LooksLikeConnectionString(string value)
        {
            return value.Contains('=');
        }
    }
}
