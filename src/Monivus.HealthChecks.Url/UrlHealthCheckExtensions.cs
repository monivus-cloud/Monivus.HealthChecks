using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.Url;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Extension methods for registering URL health checks.
    /// </summary>
    public static class UrlHealthCheckExtensions
    {
        /// <summary>
        /// Registers a URL health check entry with the provided <see cref="IHealthChecksBuilder"/>.
        /// Binds configuration from the "Monivus:Urls:{name}" section to <see cref="UrlHealthCheckOptions"/>
        /// and creates a <see cref="UrlHealthCheck"/> using the configured URL or the supplied <paramref name="url"/>.
        /// </summary>
        /// <param name="builder">The health checks builder to add the registration to.</param>
        /// <param name="name">The unique name for the health check and the configuration key under "Monivus:Urls".</param>
        /// <param name="url">An optional fallback absolute HTTP/HTTPS URL to check if not provided in configuration.</param>
        /// <param name="failureStatus">Optional <see cref="HealthStatus"/> to report when the check fails.</param>
        /// <param name="tags">Optional tags to associate with the registration; the "Url" tag is prepended automatically.</param>
        /// <param name="timeout">Optional timeout used to override the configured request timeout and passed to the registration.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="builder"/> is null, <paramref name="name"/> is null or whitespace,
        /// or if no URL is provided via configuration or the <paramref name="url"/> parameter.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown if the provided URL is not an absolute HTTP/HTTPS URL.</exception>
        public static IHealthChecksBuilder AddUrlEntry(
            this IHealthChecksBuilder builder,
            string name,
            string? url = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            builder.Services
                .AddOptions<UrlHealthCheckOptions>()
                .BindConfiguration($"Monivus:Urls:{name}");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<UrlHealthCheckOptions>>()?.Value ?? new UrlHealthCheckOptions();

                    var finalUrl = opts.Url ?? url;

                    ArgumentNullException.ThrowIfNull(finalUrl, "Url must be provided in configuration or parameters");

                    if (!IsValidHttpUrl(finalUrl))
                        throw new ArgumentException("Url must be an absolute HTTP/HTTPS URL", nameof(url));

                    if (timeout.HasValue)
                    {
                        opts.RequestTimeout = timeout.Value;
                    }

                    return new UrlHealthCheck(finalUrl, opts);
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
