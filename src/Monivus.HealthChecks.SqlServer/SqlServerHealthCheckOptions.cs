namespace Monivus.HealthChecks.SqlServer
{
    public class SqlServerHealthCheckOptions
    {
        public string? ConnectionStringOrName { get; set; }
        public string CommandText { get; set; } = "SELECT 1";
        public int? CommandTimeout { get; set; }
    }
}
