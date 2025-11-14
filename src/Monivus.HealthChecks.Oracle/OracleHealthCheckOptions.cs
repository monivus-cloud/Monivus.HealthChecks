namespace Monivus.HealthChecks.Oracle
{
    /// <summary>
    /// Provides configuration options for the Oracle health check.
    /// </summary>
    public class OracleHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the Oracle connection string or the name of a configured connection string.
        /// </summary>
        public string ConnectionStringOrName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the SQL command text to execute when probing Oracle.
        /// </summary>
        public string CommandText { get; set; } = "SELECT 1 FROM DUAL";

        /// <summary>
        /// Gets or sets the command timeout, in seconds, for the probe command.
        /// </summary>
        public int? CommandTimeout { get; set; }
    }
}

