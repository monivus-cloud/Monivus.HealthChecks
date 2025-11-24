using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Monivus.HealthChecks.MongoDb;
using MongoDB.Driver;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for registering MongoDB health checks.
    /// </summary>
    public static class MongoDbHealthCheckExtensions
    {
        /// <summary>
        /// Adds a MongoDB health check registration to the provided <see cref="IHealthChecksBuilder"/>.
        /// Binds <see cref="MongoDbHealthCheckOptions"/> from configuration path "Monivus:MongoDb".
        /// </summary>
        /// <param name="builder">The health checks builder to add the registration to.</param>
        /// <param name="name">The registration name. Defaults to "MongoDb".</param>
        /// <param name="failureStatus">The <see cref="HealthStatus"/> to report when the check fails.</param>
        /// <param name="tags">Optional tags to associate with the health check.</param>
        /// <param name="timeout">Optional timeout for the health check execution.</param>
        /// <returns>The same <see cref="IHealthChecksBuilder"/> instance for chaining.</returns>
        public static IHealthChecksBuilder AddMongoDbEntry(
            this IHealthChecksBuilder builder,
            string name = "MongoDb",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services
                .AddOptions<MongoDbHealthCheckOptions>()
                .BindConfiguration("Monivus:MongoDb");

            return builder.Add(new HealthCheckRegistration(
                name,
                sp =>
                {
                    var opts = sp.GetService<IOptions<MongoDbHealthCheckOptions>>()?.Value ?? new MongoDbHealthCheckOptions();

                    if (string.IsNullOrWhiteSpace(opts.DatabaseName))
                    {
                        opts.DatabaseName = "admin";
                    }

                    var connectionString = string.IsNullOrWhiteSpace(opts.ConnectionStringOrName)
                        ? null
                        : ResolveConnectionString(sp, opts.ConnectionStringOrName);

                    IMongoClient client;

                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        client = new MongoClient(connectionString);
                    }
                    else
                    {
                        client = sp.GetService<IMongoClient>() ??
                            throw new InvalidOperationException("Provide Monivus:MongoDb:ConnectionStringOrName or register IMongoClient.");
                    }

                    return new MongoDbHealthCheck(client, opts);
                },
                failureStatus,
                PrependTypeTag("MongoDb", tags),
                timeout));
        }

        private static IEnumerable<string> PrependTypeTag(string code, IEnumerable<string>? tags)
        {
            yield return code;
            if (tags is null) yield break;
            foreach (var t in tags) yield return t;
        }

        private static string ResolveConnectionString(IServiceProvider sp, string connectionStringOrName)
        {
            var configuration = sp.GetService<IConfiguration>();

            if (configuration != null)
            {
                var byName = configuration.GetConnectionString(connectionStringOrName);
                if (!string.IsNullOrWhiteSpace(byName))
                {
                    return byName!;
                }
            }

            return connectionStringOrName;
        }
    }
}
