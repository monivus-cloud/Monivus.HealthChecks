namespace Monivus.HealthChecks.SqlServer
{
    /// <summary>
    /// Provides configuration options for the SQL Server health check, allowing customization of
    /// </summary>
    public class SqlServerHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the SQL Server connection string or the name of a connection string
        /// </summary>
        public string ConnectionStringOrName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the SQL command text to be executed for the health check.
        /// </summary>
        public string CommandText { get; set; } = "SELECT 1";

        /// <summary>
        /// Gets or sets the command timeout in seconds for the SQL command.
        /// </summary>
        public int? CommandTimeout { get; set; }
    }
}
