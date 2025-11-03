namespace Monivus.HealthChecks.Url
{
    public class UrlHealthCheckOptions
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        // Hard request timeout for the HTTP call
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(2);

        public ISet<int>? ExpectedStatusCodes { get; set; }

        // If set, responses taking longer than this are reported Degraded
        public TimeSpan? SlowResponseThreshold { get; set; }
    }
}
