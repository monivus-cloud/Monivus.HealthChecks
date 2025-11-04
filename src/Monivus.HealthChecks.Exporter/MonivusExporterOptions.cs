namespace Monivus.HealthChecks.Exporter
{
    public class MonivusExporterOptions
    {
        public bool Enabled { get; set; } = true;

        public string ApplicationHealthCheckUrl { get; set; } = default!;

        public string MonivusCloudUrl { get; set; } = default!;

        public string? ApiKey { get; set; }

        public int CheckInterval { get; set; } = 1;

        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
