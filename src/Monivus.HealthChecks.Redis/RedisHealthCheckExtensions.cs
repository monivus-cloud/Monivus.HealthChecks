using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.Redis;
using StackExchange.Redis;

namespace Monivus.HealthChecks
{
    public static class RedisHealthCheckExtensions
    {
        /// <summary>
        /// Adds a health check for Redis using the provided connection multiplexer
        /// </summary>
        /// <param name="builder">The health checks builder</param>
        /// <param name="name">The health check name (optional)</param>
        /// <param name="failureStatus">The status to report when the health check fails (optional)</param>
        /// <param name="tags">A list of tags that can be used to filter health checks (optional)</param>
        /// <param name="timeout">The health check timeout (optional)</param>
        /// <returns>The health checks builder</returns>
        public static IHealthChecksBuilder AddRedisEntry(
            this IHealthChecksBuilder builder,
            string name = "Redis",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<RedisHealthCheckOptions>()
                .BindConfiguration($"Monivus:Redis");

            // Register the health check with the provided parameters
            var registration = new HealthCheckRegistration(
                name,
                serviceProvider =>
                {
                    var opts = serviceProvider.GetService<IOptions<RedisHealthCheckOptions>>()?.Value ?? new RedisHealthCheckOptions();

                    var redisConnection = string.IsNullOrEmpty(opts.ConnectionString) ?
                        serviceProvider.GetRequiredService<IConnectionMultiplexer>() :
                        ConnectionMultiplexer.Connect(opts.ConnectionString);

                    return new RedisHealthCheck(redisConnection, opts);
                },
                failureStatus,
                PrependTypeTag("Redis", tags),
                timeout);

            return builder.Add(registration);
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code.ToString();
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
        }
    }
}
