using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Monivus.HealthChecks.PostgreSql
{
    /// <summary>
    /// Provides a health check for monitoring the status of a PostgreSQL database.
    /// </summary>
    public class PostgreSqlHealthCheck : IHealthCheck
    {
        private readonly PostgreSqlHealthCheckOptions _options;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlHealthCheck"/> class.
        /// </summary>
        /// <param name="options">The health check options.</param>
        /// <param name="connectionString">The resolved connection string.</param>
        public PostgreSqlHealthCheck(PostgreSqlHealthCheckOptions options, string connectionString)
        {
            _options = options;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Performs a health check on the PostgreSQL database.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var openWatch = Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                openWatch.Stop();

                using var command = new NpgsqlCommand(_options.CommandText, connection);

                if (_options.CommandTimeout.HasValue)
                {
                    command.CommandTimeout = _options.CommandTimeout.Value;
                }

                var queryWatch = Stopwatch.StartNew();
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                queryWatch.Stop();

                if (result != null && result != DBNull.Value)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "ConnectionTimeout", connection.ConnectionTimeout },
                        { "State", connection.State },
                        { "CommandTimeout", command.CommandTimeout },
                        { "ConnectionOpenMilliseconds", Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                    };

                    return HealthCheckResult.Healthy("PostgreSQL is healthy and running.", data);
                }

                return HealthCheckResult.Unhealthy(
                    "PostgreSQL test query returned an unexpected result.",
                    null,
                    new Dictionary<string, object>
                    {
                        { "ConnectionOpenMilliseconds", Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    });
            }
            catch (PostgresException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "PostgreSQL access failure.",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "SqlState", ex.SqlState },
                        { "MessageText", ex.MessageText },
                        { "Severity", ex.Severity },
                        { "SchemaName", ex.SchemaName ?? string.Empty },
                        { "TableName", ex.TableName ?? string.Empty },
                        { "ColumnName", ex.ColumnName ?? string.Empty },
                        { "ConstraintName", ex.ConstraintName ?? string.Empty }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "PostgreSQL access failure.",
                    ex);
            }
        }
    }
}
