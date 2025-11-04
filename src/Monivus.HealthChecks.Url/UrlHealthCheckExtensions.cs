using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.Url;

namespace Monivus.HealthChecks
{
    public static class UrlHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddUrlEntry(
            this IHealthChecksBuilder builder,
            string url,
            string name,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            if (!IsValidHttpUrl(url))
                throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));

            builder.Services
                .AddOptions<UrlHealthCheckOptions>()
                .BindConfiguration($"Monivus:Url:{name}");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<UrlHealthCheckOptions>>()?.Value ?? new UrlHealthCheckOptions();

                    if (timeout.HasValue)
                    {
                        opts.RequestTimeout = timeout.Value;
                    }
                    
                    return new UrlHealthCheck(url, opts);
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
