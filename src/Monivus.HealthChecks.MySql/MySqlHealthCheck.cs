using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

namespace Monivus.HealthChecks.MySql
{
    /// <summary>
    /// Provides a health check for monitoring the status of a MySQL database.
    /// </summary>
    public class MySqlHealthCheck : IHealthCheck
    {
        private readonly MySqlHealthCheckOptions _options;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlHealthCheck"/> class.
        /// </summary>
        /// <param name="options">Health check options.</param>
        /// <param name="connectionString">The resolved connection string.</param>
        public MySqlHealthCheck(MySqlHealthCheckOptions options, string connectionString)
        {
            _options = options;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Performs a health check on the MySQL database.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var openWatch = Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                openWatch.Stop();

                using var command = new MySqlCommand(_options.CommandText, connection);

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

                    return HealthCheckResult.Healthy("MySQL is healthy and running.", data);
                }

                return HealthCheckResult.Unhealthy(
                    "MySQL test query returned an unexpected result.",
                    null,
                    new Dictionary<string, object>
                    {
                        { "ConnectionOpenMilliseconds", Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    });
            }
            catch (MySqlException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "MySQL connection failure.",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "ErrorCode", ex.ErrorCode },
                        { "Number", ex.Number },
                        { "SqlState", ex.SqlState ?? string.Empty },
                        { "IsTransient", ex.IsTransient }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "MySQL access failure.",
                    ex);
            }
        }
    }
}
