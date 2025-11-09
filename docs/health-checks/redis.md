---
icon: material/menu-right
---

# Redis Health Check

Pings Redis and evaluates responsiveness. Also collects useful metrics (clients, memory, ops/sec, etc.).

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddRedisEntry(name: "Redis");
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

## Example Entry (JSON)

`entries.Redis` inside the `/health` response when latency is above threshold (Degraded):

```json
{
  "status": "Degraded",
  "description": "Redis ping exceeded threshold (210ms).",
  "duration": "00:00:00.0040000",
  "durationMs": 4,
  "data": {
    "isConnected": true,
    "serverVersion": "7.2.4",
    "serverType": "Standalone",
    "pingMilliseconds": 210,
    "databaseSize": 1024
  },
  "exception": null,
  "tags": ["Redis"],
  "entryType": "Redis"
}
```