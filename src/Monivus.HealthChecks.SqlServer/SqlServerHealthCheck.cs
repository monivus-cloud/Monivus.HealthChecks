using Microsoft.Data.SqlClient;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.SqlServer
{
    /// <summary>
    /// Provides a health check for monitoring the status of a SQL Server database.
    /// </summary>
    public class SqlServerHealthCheck : IHealthCheck
    {
        private readonly SqlServerHealthCheckOptions _options;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerHealthCheck"/> class.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="connectionString"></param>
        public SqlServerHealthCheck(SqlServerHealthCheckOptions options, string connectionString)
        {
            _options = options;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Performs a health check on the SQL Server database.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var openWatch = Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                openWatch.Stop();

                using var command = new SqlCommand(_options.CommandText, connection);

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

                    return HealthCheckResult.Healthy("SqlServer is healthy and running.", data);
                }

                return HealthCheckResult.Unhealthy(
                    "SQL Server test query returned an unexpected result.",
                    null,
                    new Dictionary<string, object>
                    {
                        { "ConnectionOpenMilliseconds", Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    });
            }
            catch (SqlException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server connection failure.",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "ErrorNumber", ex.Number },
                        { "ErrorMessage", ex.Message },
                        { "Server", ex.Server },
                        { "Procedure", ex.Procedure ?? string.Empty },
                        { "LineNumber", ex.LineNumber },
                        { "ClientConnectionId", ex.ClientConnectionId.ToString() }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "SQL Server access failure.",
                    ex);
            }
        }
    }
}

