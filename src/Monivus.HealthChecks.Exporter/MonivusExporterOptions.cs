namespace Monivus.HealthChecks.Exporter
{
    public class MonivusExporterOptions
    {
        public const string SectionName = "Monivus:Exporter";

        public bool Enabled { get; set; } = true;

        public string TargetApplicationUrl { get; set; } = string.Empty;

        public string HealthCheckPath { get; set; } = "/health";

        public string MonivusCloudUrl { get; set; } = string.Empty;

        public string? ApiKey { get; set; }

        public string ApiKeyScheme { get; set; } = "ApiKey";

        public string ApiKeyHeaderName { get; set; } = "Authorization";

        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);

        public int? CheckIntervalMinutes
        {
            get => CheckInterval == TimeSpan.Zero ? null : (int)Math.Round(CheckInterval.TotalMinutes);
            set
            {
                if (value.HasValue && value.Value > 0)
                {
                    CheckInterval = TimeSpan.FromMinutes(value.Value);
                }
            }
        }

        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

        internal void Normalize()
        {
            if (string.IsNullOrWhiteSpace(HealthCheckPath))
            {
                HealthCheckPath = "/health";
            }

            if (CheckInterval <= TimeSpan.Zero)
            {
                CheckInterval = TimeSpan.FromMinutes(1);
            }

            if (HttpTimeout <= TimeSpan.Zero)
            {
                HttpTimeout = TimeSpan.FromSeconds(30);
            }
        }
    }
}
