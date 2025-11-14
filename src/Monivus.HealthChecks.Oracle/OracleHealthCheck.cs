using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oracle.ManagedDataAccess.Client;

namespace Monivus.HealthChecks.Oracle
{
    /// <summary>
    /// Provides a health check for monitoring the status of an Oracle database.
    /// </summary>
    public class OracleHealthCheck : IHealthCheck
    {
        private readonly OracleHealthCheckOptions _options;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleHealthCheck"/> class.
        /// </summary>
        /// <param name="options">The health check options.</param>
        /// <param name="connectionString">The resolved connection string.</param>
        public OracleHealthCheck(OracleHealthCheckOptions options, string connectionString)
        {
            _options = options;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Performs the health check against the Oracle database.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new OracleConnection(_connectionString);
                var openWatch = Stopwatch.StartNew();
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                openWatch.Stop();

                using var command = connection.CreateCommand();
                command.CommandText = _options.CommandText;

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

                    return HealthCheckResult.Healthy("Oracle is healthy and running.", data);
                }

                return HealthCheckResult.Unhealthy(
                    "Oracle test query returned an unexpected result.",
                    null,
                    new Dictionary<string, object>
                    {
                        { "ConnectionOpenMilliseconds", Math.Round(openWatch.Elapsed.TotalMilliseconds, 2) },
                        { "QueryDurationMilliseconds", Math.Round(queryWatch.Elapsed.TotalMilliseconds, 2) },
                        { "TestQueryResult", result?.ToString() ?? string.Empty }
                    });
            }
            catch (OracleException ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Oracle access failure.",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "Number", ex.Number },
                        { "DataSource", ex.DataSource ?? string.Empty },
                        { "Procedure", ex.Procedure ?? string.Empty }
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Oracle access failure.",
                    ex);
            }
        }
    }
}

