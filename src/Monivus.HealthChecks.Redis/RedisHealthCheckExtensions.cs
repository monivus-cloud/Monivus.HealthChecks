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
        /// Registers a Redis health check with the provided <see cref="IHealthChecksBuilder"/>.
        /// Binds <see cref="RedisHealthCheckOptions"/> from configuration section "Monivus:Redis".
        /// If <see cref="RedisHealthCheckOptions.ConnectionString"/> is not set, the health check
        /// will use an <see cref="IConnectionMultiplexer"/> resolved from DI; otherwise it will
        /// create a new <see cref="ConnectionMultiplexer"/> using the configured connection string.
        /// </summary>
        /// <param name="builder">The health checks builder to add the Redis health check to.</param>
        /// <param name="name">The registration name for the health check. Defaults to "Redis".</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the check fails. If null, the default is used.</param>
        /// <param name="tags">Optional tags to associate with the health check.</param>
        /// <param name="timeout">An optional timeout to apply to the health check execution.</param>
        /// <returns>The original <see cref="IHealthChecksBuilder"/>, allowing further configuration chaining.</returns>
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
