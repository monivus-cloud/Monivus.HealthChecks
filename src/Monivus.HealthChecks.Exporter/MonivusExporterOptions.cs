namespace Monivus.HealthChecks.Exporter
{
    /// <summary>
    /// Options for configuring the Monivus health checks exporter.
    /// </summary>
    public class MonivusExporterOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the exporter is enabled.
        /// Defaults to <c>true</c>.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the URL of the application's health check endpoint that will be queried.
        /// This value must be a valid absolute URL.
        /// </summary>
        public string ApplicationHealthCheckUrl { get; set; } = default!;

        /// <summary>
        /// Gets or sets the base URL of the Monivus cloud API where results should be sent.
        /// This value must be a valid absolute URL.
        /// </summary>
        public string MonivusCloudUrl { get; set; } = default!;

        /// <summary>
        /// Gets or sets the optional API key used to authenticate with the Monivus cloud API.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the interval in minutes between consecutive health checks.
        /// Defaults to <c>1</c> (one minute).
        /// </summary>
        public int CheckInterval { get; set; } = 1;

        /// <summary>
        /// Gets or sets the HTTP request timeout for calls to the health check endpoint.
        /// Defaults to 5 seconds.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
