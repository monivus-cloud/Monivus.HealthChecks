---
icon: material/menu-right
---

# Redis Health Check

Pings Redis and evaluates responsiveness. Also collects useful metrics (clients, memory, ops/sec, etc.).

## Install

```bash
dotnet add package Monivus.HealthChecks.Redis
```

## Usage

```csharp
using Monivus.HealthChecks;

builder.Services.AddHealthChecks()
    .AddRedisEntry();
```

If not providing a connection string in configuration, ensure an `IConnectionMultiplexer` is registered.

## Configuration

Binding path: `Monivus:Redis`

### ConnectionString
Gets or sets the Redis connection string.

```json
{
  "Monivus": {
	"Redis": { 
		"ConnectionString": "localhost:6379" 
		} 
	}
}
```

### SlowPingThresholdMilliseconds
Threshold in milliseconds for considering a Redis PING as slow.

Status rules:

- Healthy when the PING completes and (if set) the latency is less than or equal to the threshold.
- Degraded when the measured PING latency is greater than the threshold.
- Unhealthy when the connection is not established, there are no endpoints, or exceptions occur.

```json
{
  "Monivus": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "SlowPingThresholdMilliseconds": 200
    }
  }
}
```

Example interpretation: if a `PING` takes above 200 ms, the check reports Degraded; at or below 200 ms it’s Healthy. Connection or command errors result in Unhealthy.

## Admin Mode (AllowAdmin)

Some extended metrics are retrieved via the Redis `INFO` command. In StackExchange.Redis this is treated as a server/admin operation. If the connection has admin operations disabled (`AllowAdmin = false`, which is the default) or the command is unavailable, attempting to call `server.InfoAsync()` may throw.

This health check handles that gracefully:

- It still performs a `PING` and basic probes (e.g., server info, DB size, last save) and reports Healthy/Degraded based on latency and connectivity.
- When admin is not allowed, it returns Healthy with limited data and the description includes a hint: `Redis is healthy and responsive. (Admin not allowed!)`.
- Extended metrics (memory usage breakdown, connected clients, ops/sec, hit/miss, etc.) are omitted in this mode.

To enable extended metrics, allow admin commands for the connection used by the health check:

```ini
# Connection string option
localhost:6379,allowAdmin=true
```

```csharp
// ConfigurationOptions example
var opts = ConfigurationOptions.Parse("localhost:6379");
opts.AllowAdmin = true;
var mux = await ConnectionMultiplexer.ConnectAsync(opts);

builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
builder.Services.AddHealthChecks().AddRedisEntry("Redis");
```

Tip: If you prefer to keep application connections non-admin, you can maintain a separate admin-enabled multiplexer registered only for health/metrics.

## Example Entries (JSON)

Below are example payloads for `entries.Redis` as returned by the `/health` endpoint.

Healthy with admin enabled (full metrics):

```json
{
  "status": "Healthy",
  "description": "Redis is healthy and responsive.",
  "duration": "00:00:00.0030000",
  "durationMs": 3,
  "data": {
    "IsConnected": true,
    "ServerVersion": "7.2.4",
    "ServerType": "Standalone",
    "PingMilliseconds": 12,
    "DatabaseSize": 2048,
    "LastSaveUtc": "2024-06-12T08:21:45.0000000Z",
    "UsedMemoryMb": 128.5,
    "UsedMemoryRssMb": 140.1,
    "TotalSystemMemoryMb": 16384,
    "MemoryUsagePercent": 0.78,
    "ConnectedClients": 5,
    "BlockedClients": 0,
    "OpsPerSecond": 342,
    "UptimeSeconds": 86400,
    "MemoryFragmentationRatio": 1.07,
    "KeyspaceHits": 15000,
    "KeyspaceMisses": 500,
    "KeyspaceHitRatePercent": 96.77
  },
  "exception": null,
  "tags": ["Redis"],
  "entryType": "Redis"
}
```

Healthy with admin disabled (limited metrics):

```json
{
  "status": "Healthy",
  "description": "Redis is healthy and responsive. (Admin not allowed!)",
  "duration": "00:00:00.0020000",
  "durationMs": 2,
  "data": {
    "IsConnected": true,
    "ServerVersion": "7.2.4",
    "ServerType": "Standalone",
    "PingMilliseconds": 10,
    "DatabaseSize": 1024,
    "LastSaveUtc": "2024-06-12T08:21:45.0000000Z"
  },
  "exception": null,
  "tags": ["Redis"],
  "entryType": "Redis"
}
```

Degraded when ping exceeds threshold (no extended metrics, matches current implementation order):

```json
{
  "status": "Degraded",
  "description": "Redis ping exceeded threshold (210ms).",
  "duration": "00:00:00.0040000",
  "durationMs": 4,
  "data": {
    "IsConnected": true,
    "ServerVersion": "7.2.4",
    "ServerType": "Standalone",
    "PingMilliseconds": 210,
    "DatabaseSize": 1024,
    "LastSaveUtc": "2024-06-12T08:21:45.0000000Z"
  },
  "exception": null,
  "tags": ["Redis"],
  "entryType": "Redis"
}
```
