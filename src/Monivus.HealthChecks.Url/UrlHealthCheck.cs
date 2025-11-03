using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks.Url
{
    public sealed class UrlHealthCheck : IHealthCheck
    {
        private readonly UrlHealthCheckOptions _options;
        private readonly string _url;

        public UrlHealthCheck(string url, UrlHealthCheckOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Url must be provided", nameof(url));
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));
            }
            _url = url;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var http = new HttpClient
            {
                Timeout = _options.RequestTimeout
            };

            using var request = new HttpRequestMessage(_options.Method, _url);

            try
            {
                var sw = Stopwatch.StartNew();
                using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
                sw.Stop();

                var code = (int)response.StatusCode;
                var inExpected = _options.ExpectedStatusCodes?.Contains(code)
                                  ?? (code >= 200 && code < 300);

                var data = new Dictionary<string, object>
                {
                    ["Url"] = _url,
                    ["Method"] = _options.Method.Method,
                    ["StatusCode"] = code,
                    ["ReasonPhrase"] = response.ReasonPhrase ?? string.Empty
                };

                if (inExpected)
                {
                    if (_options.SlowResponseThreshold.HasValue && sw.Elapsed > _options.SlowResponseThreshold.Value)
                    {
                        return new HealthCheckResult(
                            HealthStatus.Degraded,
                            $"Response exceeded slow-response threshold of {_options.SlowResponseThreshold.Value.TotalMilliseconds} ms",
                            exception: null,
                            data: data);
                    }

                    return HealthCheckResult.Healthy(null, data);
                }

                return HealthCheckResult.Unhealthy(
                    $"Unexpected status code: {code}",
                    null,
                    data);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy(
                    "Request timed out",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["Url"] = _url,
                        ["Method"] = _options.Method.Method,
                        ["RequestTimeoutSeconds"] = Math.Round(_options.RequestTimeout.TotalSeconds, 2)
                    });
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "HTTP request failed",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["Url"] = _url,
                        ["Method"] = _options.Method.Method
                    });
            }
        }
    }
}
