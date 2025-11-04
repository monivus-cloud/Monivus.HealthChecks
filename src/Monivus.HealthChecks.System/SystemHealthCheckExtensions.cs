using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.System;

namespace Monivus.HealthChecks
{
    public static class SystemHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddSystemEntry(
            this IHealthChecksBuilder builder,
            string name = "System",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<SystemHealthCheckOptions>()
                .BindConfiguration($"Monivus:System");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<SystemHealthCheckOptions>>()?.Value ?? new SystemHealthCheckOptions();

                    return new SystemHealthCheck(opts);
                },
                failureStatus,
                PrependTypeTag("System", tags),
                timeout));
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code.ToString();
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
        }
    }
}
