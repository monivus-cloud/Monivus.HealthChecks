using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Monivus.HealthChecks.Redis
{
    /// <summary>
    /// Performs a health check for a Redis connection, evaluating its connectivity, responsiveness, and key metrics.
    /// </summary>
    /// <remarks>This health check verifies the connection state of the Redis server, performs a ping to
    /// measure responsiveness,  and collects various metrics such as memory usage, connected clients, and uptime. If
    /// the connection is slow or  unhealthy, the health check result will reflect the degraded or unhealthy state,
    /// respectively.</remarks>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly RedisHealthCheckOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
        /// </summary>
        /// <param name="redisConnection">The Redis connection multiplexer used to interact with the Redis server. Cannot be <see langword="null"/>.</param>
        /// <param name="options">The configuration options for the health check. May be <see langword="null"/> to use default settings.</param>
        public RedisHealthCheck(IConnectionMultiplexer redisConnection, RedisHealthCheckOptions options)
        {
            ArgumentNullException.ThrowIfNull(redisConnection);

            _redisConnection = redisConnection;
            _options = options;
        }

        /// <summary>
        /// Performs a health check on the Redis connection.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_redisConnection.IsConnected)
                {
                    return HealthCheckResult.Unhealthy(
                        "Redis connection is not established.",
                        exception: null,
                        data: new Dictionary<string, object>
                        {
                            ["ConnectionState"] = "Disconnected",
                            ["TimestampUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                        });
                }

                var database = _redisConnection.GetDatabase();
                var endpoints = _redisConnection.GetEndPoints();

                if (endpoints.Length == 0)
                {
                    return HealthCheckResult.Unhealthy(
                        "Redis connection has no configured endpoints.");
                }

                var endpoint = endpoints[0];
                var server = _redisConnection.GetServer(endpoint);

                var pingResponse = await database.PingAsync().ConfigureAwait(false);
                var databaseSize = await server.DatabaseSizeAsync().ConfigureAwait(false);
                var lastSave = await server.LastSaveAsync().ConfigureAwait(false);

                var healthData = new Dictionary<string, object>
                {
                    ["IsConnected"] = server.IsConnected,
                    ["ServerVersion"] = server.Version.ToString(),
                    ["ServerType"] = server.ServerType.ToString(),
                    ["PingMilliseconds"] = Math.Round(pingResponse.TotalMilliseconds, 2),
                    ["DatabaseSize"] = databaseSize,
                    ["LastSaveUtc"] = lastSave.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                };

                if (_options.SlowPingThresholdMilliseconds.HasValue
                    && pingResponse.TotalMilliseconds > _options.SlowPingThresholdMilliseconds.Value)
                {
                    return HealthCheckResult.Degraded(
                        $"Redis ping exceeded threshold ({pingResponse.TotalMilliseconds}ms).",
                        data: healthData);
                }

                var allowAdmin = false;

                try
                {
                    var serverInfoSections = await server.InfoAsync().ConfigureAwait(false);
                    var infoValues = FlattenInfo(serverInfoSections);

                    BuildInfoData(infoValues, healthData);
                    allowAdmin = true;
                }
                catch 
                {
                    // Ignore info retrieval errors, admin mode may be disabled
                }

                return HealthCheckResult.Healthy(
                    allowAdmin ? "Redis is healthy and responsive." : "Redis is healthy and responsive. (Admin not allowed!)",
                    healthData);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Redis health check failed.",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["ErrorMessage"] = ex.Message,
                        ["ExceptionType"] = ex.GetType().FullName ?? ex.GetType().Name,
                        ["TimestampUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                    });
            }
        }

        private static void BuildInfoData(
            IReadOnlyDictionary<string, string> infoValues,
            Dictionary<string, object> healthData)
        {
            var usedMemoryBytes = TryGetInfoInt64(infoValues, "used_memory");
            var usedMemoryRssBytes = TryGetInfoInt64(infoValues, "used_memory_rss");
            var systemMemoryBytes = TryGetInfoInt64(infoValues, "total_system_memory");
            var connectedClients = TryGetInfoInt64(infoValues, "connected_clients");
            var blockedClients = TryGetInfoInt64(infoValues, "blocked_clients");
            var opsPerSecond = TryGetInfoInt64(infoValues, "instantaneous_ops_per_sec");
            var keyspaceHits = TryGetInfoInt64(infoValues, "keyspace_hits");
            var keyspaceMisses = TryGetInfoInt64(infoValues, "keyspace_misses");
            var uptimeSeconds = TryGetInfoInt64(infoValues, "uptime_in_seconds");
            var memFragmentation = TryGetInfoDouble(infoValues, "mem_fragmentation_ratio");

            if (usedMemoryBytes.HasValue)
            {
                healthData["UsedMemoryMb"] = Math.Round(usedMemoryBytes.Value / 1024d / 1024d, 2);
            }

            if (usedMemoryRssBytes.HasValue)
            {
                healthData["UsedMemoryRssMb"] = Math.Round(usedMemoryRssBytes.Value / 1024d / 1024d, 2);
            }

            if (systemMemoryBytes.HasValue)
            {
                healthData["TotalSystemMemoryMb"] = Math.Round(systemMemoryBytes.Value / 1024d / 1024d, 2);
                if (usedMemoryBytes.HasValue && systemMemoryBytes.Value > 0)
                {
                    healthData["MemoryUsagePercent"] = Math.Round(
                        usedMemoryBytes.Value / (double)systemMemoryBytes.Value * 100, 2);
                }
            }

            if (connectedClients.HasValue)
            {
                healthData["ConnectedClients"] = connectedClients.Value;
            }

            if (blockedClients.HasValue)
            {
                healthData["BlockedClients"] = blockedClients.Value;
            }

            if (opsPerSecond.HasValue)
            {
                healthData["OpsPerSecond"] = opsPerSecond.Value;
            }

            if (uptimeSeconds.HasValue)
            {
                healthData["UptimeSeconds"] = uptimeSeconds.Value;
            }

            if (memFragmentation.HasValue)
            {
                healthData["MemoryFragmentationRatio"] = Math.Round(memFragmentation.Value, 2);
            }

            var totalKeyspaceOperations = (keyspaceHits ?? 0) + (keyspaceMisses ?? 0);
            if (totalKeyspaceOperations > 0)
            {
                healthData["KeyspaceHits"] = keyspaceHits ?? 0;
                healthData["KeyspaceMisses"] = keyspaceMisses ?? 0;
                healthData["KeyspaceHitRatePercent"] = Math.Round(
                    (keyspaceHits ?? 0) / (double)totalKeyspaceOperations * 100,
                    2);
            }
        }

        private static IReadOnlyDictionary<string, string> FlattenInfo(IEnumerable<IEnumerable<KeyValuePair<string, string>>> sections)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (sections == null)
            {
                return result;
            }

            foreach (var section in sections)
            {
                if (section == null)
                {
                    continue;
                }

                foreach (var pair in section)
                {
                    if (!result.ContainsKey(pair.Key))
                    {
                        result[pair.Key] = pair.Value;
                    }
                }
            }

            return result;
        }

        private static string? GetInfoValue(IReadOnlyDictionary<string, string> infoValues, string key)
        {
            if (infoValues != null &&
                infoValues.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return null;
        }

        private static long? TryGetInfoInt64(IReadOnlyDictionary<string, string> infoValues, string key)
        {
            var value = GetInfoValue(infoValues, key);
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static double? TryGetInfoDouble(IReadOnlyDictionary<string, string> infoValues, string key)
        {
            var value = GetInfoValue(infoValues, key);
            return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}
