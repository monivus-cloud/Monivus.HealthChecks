namespace Monivus.HealthChecks.Url
{
    public class UrlHealthCheckOptions
    {
        public string? Url { get; set; }

        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public TimeSpan? RequestTimeout { get; set; }

        public HashSet<int>? ExpectedStatusCodes { get; set; }

        public TimeSpan? SlowResponseThreshold { get; set; }
    }
}
