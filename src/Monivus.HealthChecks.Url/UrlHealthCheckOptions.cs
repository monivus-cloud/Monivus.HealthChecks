namespace Monivus.HealthChecks.Url
{
    /// <summary>
    /// Provides configuration options for the URL health check, allowing customization of
    /// </summary>
    public class UrlHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the absolute HTTP/HTTPS URL to monitor.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the HTTP method to use when making the request. Defaults to GET.
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Gets or sets the timeout duration for the HTTP request.
        /// </summary>
        public TimeSpan? RequestTimeout { get; set; }

        /// <summary>
        /// Gets or sets the set of expected HTTP status codes that indicate a healthy response.
        /// </summary>
        public HashSet<int>? ExpectedStatusCodes { get; set; }

        /// <summary>
        /// Gets or sets the threshold duration for considering a response as slow.
        /// </summary>
        public TimeSpan? SlowResponseThreshold { get; set; }
    }
}
