using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public static class SystemHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddSystemEntry(
            this IHealthChecksBuilder builder,
            Action<SystemHealthCheckOptions>? configure = null,
            string name = "System",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.AddSystemEntry(
                _ =>
                {
                    var options = new SystemHealthCheckOptions();
                    configure?.Invoke(options);
                    return options;
                },
                name,
                failureStatus,
                PrependTypeTag("System", tags),
                timeout);
        }

        public static IHealthChecksBuilder AddSystemEntry(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, SystemHealthCheckOptions> optionsFactory,
            string name = "System",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(optionsFactory);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new SystemHealthCheck(optionsFactory(sp)),
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
