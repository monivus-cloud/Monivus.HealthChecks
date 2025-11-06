namespace Monivus.HealthChecks
{
    /// <summary>
    /// Configuration options for aggregating health checks across multiple remote endpoints.
    /// </summary>
    public class AggregatedHealthOptions
    {
        /// <summary>
        /// Single remote health endpoint URL (legacy / single-endpoint support).
        /// If set and <see cref="RemoteEndpoints"/> is empty, this value will be migrated into <see cref="RemoteEndpoints"/> by <see cref="Normalize"/>.
        /// </summary>
        public string RemoteHealthEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// The default name to use when converting <see cref="RemoteHealthEndpoint"/> into an entry in <see cref="RemoteEndpoints"/>.
        /// </summary>
        public string RemoteEntryName { get; set; } = "api";

        /// <summary>
        /// Explicit list of remote health endpoints to aggregate.
        /// </summary>
        public IList<RemoteHealthEndpoint> RemoteEndpoints { get; set; } = new List<RemoteHealthEndpoint>();

        /// <summary>
        /// Default HTTP timeout to use when contacting remote endpoints.
        /// Individual endpoints may override this via <see cref="RemoteHealthEndpoint.HttpTimeout"/>.
        /// </summary>
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// When true, include a summary entry that represents the aggregated remote health status.
        /// </summary>
        public bool IncludeRemoteSummaryEntry { get; set; } = true;

        /// <summary>
        /// Normalize options to support single-endpoint configuration.
        /// If <see cref="RemoteEndpoints"/> is empty and <see cref="RemoteHealthEndpoint"/> is provided,
        /// a corresponding <see cref="RemoteHealthEndpoint"/> will be added to <see cref="RemoteEndpoints"/>.
        /// </summary>
        internal void Normalize()
        {
            if (RemoteEndpoints.Count == 0 && !string.IsNullOrWhiteSpace(RemoteHealthEndpoint))
            {
                RemoteEndpoints.Add(new RemoteHealthEndpoint
                {
                    Url = RemoteHealthEndpoint,
                    Name = RemoteEntryName
                });
            }
        }

        /// <summary>
        /// Add a remote health endpoint to the aggregated configuration.
        /// </summary>
        /// <param name="url">Absolute HTTP/HTTPS URL of the remote health endpoint.</param>
        /// <param name="name">Logical name for the remote endpoint (used in aggregated output).</param>
        /// <param name="timeout">Optional per-endpoint HTTP timeout. If null, <see cref="HttpTimeout"/> is used.</param>
        /// <returns>The current <see cref="AggregatedHealthOptions"/> instance to allow chaining.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="url"/> or <paramref name="name"/> is null/empty, or if <paramref name="url"/> is not an absolute HTTP/HTTPS URL.
        /// </exception>
        public AggregatedHealthOptions AddEndpoint(string url, string name, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url must be provided", nameof(url));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided", nameof(name));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));
            }

            RemoteEndpoints.Add(new RemoteHealthEndpoint
            {
                Url = url,
                Name = name,
                HttpTimeout = timeout
            });
            return this;
        }

        /// <summary>
        /// Add a remote health endpoint using the provided URL for both the URL and the name.
        /// </summary>
        /// <param name="url">Absolute HTTP/HTTPS URL of the remote health endpoint.</param>
        /// <returns>The current <see cref="AggregatedHealthOptions"/> instance to allow chaining.</returns>
        public AggregatedHealthOptions AddEndpoint(string url) => AddEndpoint(url, url, null);
    }

    /// <summary>
    /// Represents a single remote health endpoint configuration.
    /// </summary>
    public class RemoteHealthEndpoint
    {
        /// <summary>
        /// Absolute HTTP/HTTPS URL of the remote health endpoint.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Logical name used in aggregated results for this endpoint.
        /// </summary>
        public string Name { get; set; } = "api";

        /// <summary>
        /// Optional per-endpoint HTTP timeout. When null, the aggregator's <see cref="AggregatedHealthOptions.HttpTimeout"/> is used.
        /// </summary>
        public TimeSpan? HttpTimeout { get; set; }
    }
}
