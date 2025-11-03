using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Monivus.HealthChecks.Url
{
    public static class UrlHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddUrlEntry(
            this IHealthChecksBuilder builder,
            string url,
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null,
            Action<UrlHealthCheckOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));
            if (!IsValidHttpUrl(url))
                throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));

            return builder.Add(new HealthCheckRegistration(
                name ?? url,
                sp =>
                {
                    var baseOpts = sp.GetService<IOptions<UrlHealthCheckOptions>>()?.Value;
                    var options = baseOpts is null
                        ? new UrlHealthCheckOptions()
                        : new UrlHealthCheckOptions
                        {
                            Method = baseOpts.Method,
                            RequestTimeout = baseOpts.RequestTimeout,
                            ExpectedStatusCodes = baseOpts.ExpectedStatusCodes is null ? null : new HashSet<int>(baseOpts.ExpectedStatusCodes),
                            SlowResponseThreshold = baseOpts.SlowResponseThreshold
                        };
                    if (timeout.HasValue)
                    {
                        options.RequestTimeout = timeout.Value;
                    }
                    configure?.Invoke(options);
                    return new UrlHealthCheck(url, options);
                },
                failureStatus,
                PrependTypeTag("Url", tags),
                timeout));
        }

        public static IHealthChecksBuilder AddUrlEntry(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, string> urlFactory,
            string name = "url",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(urlFactory);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var urlValue = urlFactory(sp);
                    if (string.IsNullOrWhiteSpace(urlValue) || !IsValidHttpUrl(urlValue))
                        throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(urlFactory));
                    var baseOpts = sp.GetService<IOptions<UrlHealthCheckOptions>>()?.Value;
                    var options = baseOpts is null
                        ? new UrlHealthCheckOptions()
                        : new UrlHealthCheckOptions
                        {
                            Method = baseOpts.Method,
                            RequestTimeout = baseOpts.RequestTimeout,
                            ExpectedStatusCodes = baseOpts.ExpectedStatusCodes is null ? null : new HashSet<int>(baseOpts.ExpectedStatusCodes),
                            SlowResponseThreshold = baseOpts.SlowResponseThreshold
                        };
                    if (timeout.HasValue)
                    {
                        options.RequestTimeout = timeout.Value;
                    }
                    return new UrlHealthCheck(urlValue, options);
                },
                failureStatus,
                PrependTypeTag("Url", tags),
                timeout));
        }

        private static bool IsValidHttpUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                   (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code.ToString();
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
        }
    }
}
