using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public static class ResourceUtilizationHealthCheckExtensions
    {
        public static IHealthChecksBuilder AddResourceUtilizationEntry(
            this IHealthChecksBuilder builder,
            Action<ResourceUtilizationHealthCheckOptions>? configure = null,
            string name = "resource_utilization",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.AddResourceUtilizationEntry(
                _ =>
                {
                    var options = new ResourceUtilizationHealthCheckOptions();
                    configure?.Invoke(options);
                    return options;
                },
                name,
                failureStatus,
                PrependTypeTag("ResourceUtilization", tags),
                timeout);
        }

        public static IHealthChecksBuilder AddResourceUtilizationEntry(
            this IHealthChecksBuilder builder,
            Func<IServiceProvider, ResourceUtilizationHealthCheckOptions> optionsFactory,
            string name = "resource_utilization",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(optionsFactory);

            return builder.Add(new HealthCheckRegistration(
                name,
                sp => new ResourceUtilizationHealthCheck(optionsFactory(sp)),
                failureStatus,
                PrependTypeTag("ResourceUtilization", tags),
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
