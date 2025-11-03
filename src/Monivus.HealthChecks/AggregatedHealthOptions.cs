namespace Monivus.HealthChecks
{
    public class AggregatedHealthOptions
    {
        public string RemoteHealthEndpoint { get; set; } = string.Empty;
        public string RemoteEntryName { get; set; } = "api";
        public IList<RemoteHealthEndpoint> RemoteEndpoints { get; set; } = [];
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(5);
        public bool IncludeRemoteSummaryEntry { get; set; } = true;

        // Normalize options to support single-endpoint configuration
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

        public AggregatedHealthOptions AddEndpoint(string url) => AddEndpoint(url, url, null);
    }

    public class RemoteHealthEndpoint
    {
        public string Url { get; set; } = string.Empty;
        public string Name { get; set; } = "api";
        public TimeSpan? HttpTimeout { get; set; }
    }
}
