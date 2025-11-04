using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Monivus.HealthChecks.SqlServer
{
    public static class SqlServerHealthCheckBuilderExtensions
    {
        public static IHealthChecksBuilder AddSqlServerEntry(
            this IHealthChecksBuilder builder,
            string name = "SqlServer",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<SqlServerHealthCheckOptions>()
                .BindConfiguration($"Monivus:SqlServer");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<SqlServerHealthCheckOptions>>()?.Value ?? new SqlServerHealthCheckOptions();

                    ArgumentNullException.ThrowIfNull(opts.ConnectionStringOrName, "ConnectionName must be provided in configuration or options");

                    var connectionString = ResolveConnectionString(sp, opts.ConnectionStringOrName);

                    return new SqlServerHealthCheck(opts, connectionString);
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

            return connectionStringOrName;
        }
    }
}
