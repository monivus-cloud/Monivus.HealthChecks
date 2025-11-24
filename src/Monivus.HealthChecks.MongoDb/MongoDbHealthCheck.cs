using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Monivus.HealthChecks.MongoDb
{
    /// <summary>
    /// Performs a health check by issuing a ping command against a MongoDB database.
    /// </summary>
    public class MongoDbHealthCheck : IHealthCheck
    {
        private readonly IMongoClient _mongoClient;
        private readonly MongoDbHealthCheckOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbHealthCheck"/> class.
        /// </summary>
        /// <param name="mongoClient">The MongoDB client used to execute commands.</param>
        /// <param name="options">The configuration options for the health check.</param>
        public MongoDbHealthCheck(IMongoClient mongoClient, MongoDbHealthCheckOptions options)
        {
            ArgumentNullException.ThrowIfNull(mongoClient);
            ArgumentNullException.ThrowIfNull(options);

            _mongoClient = mongoClient;
            _options = options;
        }

        /// <summary>
        /// Executes the MongoDB ping command and reports health based on the result.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var databaseName = string.IsNullOrWhiteSpace(_options.DatabaseName) ? "admin" : _options.DatabaseName;

            try
            {
                var database = _mongoClient.GetDatabase(databaseName);
                var pingCommand = new BsonDocument("ping", 1);

                var stopwatch = Stopwatch.StartNew();
                var result = await database.RunCommandAsync<BsonDocument>(pingCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                var ok = result != null && result.TryGetValue("ok", out var okValue) ? okValue.ToDouble() : 0d;
                var cluster = _mongoClient.Cluster.Description;

                var data = new Dictionary<string, object>
                {
                    { "Database", databaseName },
                    { "ClusterType", cluster.Type.ToString() },
                    { "ClusterState", cluster.State.ToString() },
                    { "ServerCount", cluster.Servers.Count },
                    { "PingMilliseconds", Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2) },
                    { "Ok", ok }
                };

                if (ok >= 1)
                {
                    return HealthCheckResult.Healthy("MongoDB is healthy and running.", data);
                }

                if (_options.PingLatencyThresholdMs.HasValue &&
                    stopwatch.Elapsed.TotalMilliseconds > _options.PingLatencyThresholdMs.Value)
                {
                    return HealthCheckResult.Degraded(
                        $"MongoDB ping latency exceeded the threshold of {_options.PingLatencyThresholdMs.Value} ms.",
                        exception: null, data: data);
                }

                return HealthCheckResult.Unhealthy(
                    "MongoDB ping returned an unexpected result.",
                    null, data);
            }
            catch (MongoException ex)
            {
                int? code = ex switch
                {
                    MongoCommandException cmd => cmd.Code,
                    MongoWriteException write => write.WriteError?.Code,
                    MongoBulkWriteException bulk => bulk.WriteErrors.FirstOrDefault()?.Code,
                    _ => null
                };

                var data = new Dictionary<string, object?>
                {
                    { "Message", ex.Message }
                };

                if (code.HasValue)
                {
                    data["Code"] = code.Value;
                }

                return HealthCheckResult.Unhealthy(
                    "MongoDB access failure.",
                    ex,
                    data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "MongoDB access failure.",
                    ex);
            }
        }
    }
}
