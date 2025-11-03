using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Monivus.HealthChecks.Redis
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly double _slowPingThresholdMilliseconds;

        public RedisHealthCheck(IConnectionMultiplexer redisConnection, double slowPingThresholdMilliseconds = 1000)
        {
            _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
            _slowPingThresholdMilliseconds = slowPingThresholdMilliseconds;
        }

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
                var serverInfoSections = await server.InfoAsync().ConfigureAwait(false);
                var infoValues = FlattenInfo(serverInfoSections);
                var databaseSize = await server.DatabaseSizeAsync().ConfigureAwait(false);
                var lastSave = await server.LastSaveAsync().ConfigureAwait(false);

                var healthData = BuildHealthData(server, infoValues, pingResponse.TotalMilliseconds, databaseSize, lastSave);

                if (pingResponse.TotalMilliseconds > _slowPingThresholdMilliseconds)
                {
                    return HealthCheckResult.Degraded(
                        $"Redis ping exceeded threshold ({pingResponse.TotalMilliseconds}ms).",
                        data: healthData);
                }

                return HealthCheckResult.Healthy(
                    "Redis is healthy and responsive.",
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

        private static Dictionary<string, object> BuildHealthData(
            IServer server,
            IReadOnlyDictionary<string, string> infoValues,
            double pingMilliseconds,
            long databaseSize,
            DateTime? lastSave)
        {
            var healthData = new Dictionary<string, object>
            {
                ["Endpoint"] = server.EndPoint?.ToString() ?? string.Empty,
                ["IsConnected"] = server.IsConnected,
                ["ServerVersion"] = server.Version.ToString(),
                ["ServerType"] = server.ServerType.ToString(),
                ["PingMilliseconds"] = System.Math.Round(pingMilliseconds, 2),
                ["DatabaseSize"] = databaseSize,
                ["LastSaveUtc"] = lastSave.HasValue
                    ? lastSave.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture)
                    : null!
            };

            var usedMemoryBytes = TryGetInfoInt64(infoValues, "used_memory");
            var usedMemoryRssBytes = TryGetInfoInt64(infoValues, "used_memory_rss");
            var systemMemoryBytes = TryGetInfoInt64(infoValues, "total_system_memory");
            var connectedClients = TryGetInfoInt64(infoValues, "connected_clients");
            var blockedClients = TryGetInfoInt64(infoValues, "blocked_clients");
            var opsPerSecond = TryGetInfoInt64(infoValues, "instantaneous_ops_per_sec");
            var keyspaceHits = TryGetInfoInt64(infoValues, "keyspace_hits");
            var keyspaceMisses = TryGetInfoInt64(infoValues, "keyspace_misses");
            var role = GetInfoValue(infoValues, "role");
            var uptimeSeconds = TryGetInfoInt64(infoValues, "uptime_in_seconds");
            var memFragmentation = TryGetInfoDouble(infoValues, "mem_fragmentation_ratio");

            if (usedMemoryBytes.HasValue)
            {
                healthData["UsedMemoryBytes"] = usedMemoryBytes.Value;
                healthData["UsedMemoryMegabytes"] = Math.Round(usedMemoryBytes.Value / 1024d / 1024d, 2);
            }

            if (usedMemoryRssBytes.HasValue)
            {
                healthData["UsedMemoryRssBytes"] = usedMemoryRssBytes.Value;
            }

            if (systemMemoryBytes.HasValue)
            {
                healthData["TotalSystemMemoryBytes"] = systemMemoryBytes.Value;
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

            if (!string.IsNullOrWhiteSpace(role))
            {
                healthData["ReplicationRole"] = role!;
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

            return healthData;
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
