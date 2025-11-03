namespace Monivus.HealthChecks.SqlServer
{
    public class SqlServerHealthCheckOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string TestQuery { get; set; } = "SELECT 1";
        public TimeSpan? Timeout { get; set; }
    }
}
