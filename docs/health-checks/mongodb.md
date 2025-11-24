---
icon: material/menu-right
---

# MongoDB Health Check

Runs the MongoDB `ping` command against the configured database to verify connectivity and responsiveness.

Status rules:

- Healthy when the ping command returns `ok` >= 1 and, if configured, ping latency is within the threshold.
- Degraded when the ping latency exceeds `PingLatencyThresholdMs`.
- Unhealthy when the ping fails or returns an unexpected result.

## Install

```bash
dotnet add package Monivus.HealthChecks.MongoDb
```

## Usage

```csharp
using Monivus.HealthChecks;

builder.Services.AddHealthChecks()
    .AddMongoDbEntry();
```

Configuration binding path: `Monivus:MongoDb`

## Configuration

### ConnectionStringOrName
Gets or sets the MongoDB connection string or the name of a connection string. If omitted, the health check will try to use an `IMongoClient` registered in DI.

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://myuser:mypassword@localhost:27017/?retryWrites=true&w=majority"
  },
  "Monivus": {
    "MongoDb": { "ConnectionStringOrName": "Mongo" }
  }
}
```

### DatabaseName
Gets or sets the database to target for the ping command. Defaults to `admin`.

```json
{
  "Monivus": { "MongoDb": { "DatabaseName": "admin" } }
}
```

### PingLatencyThresholdMs
Optional ping latency threshold in milliseconds. Returns Degraded when the measured ping exceeds this value.

```json
{
  "Monivus": { "MongoDb": { "PingLatencyThresholdMs": 250 } }
}
```

Example interpretation: if the driver can reach the server and the ping returns `ok: 1`, the check reports Healthy. Connection or command failures report Unhealthy.

## Example Entry (JSON)

`entries.MongoDb` inside the `/health` response when Healthy:

```json
{
  "status": "Healthy",
  "description": "MongoDB is healthy and running.",
  "duration": "00:00:00.0080000",
  "durationMs": 8,
  "data": {
    "database": "admin",
    "clusterType": "ReplicaSet",
    "clusterState": "Connected",
    "serverCount": 3,
    "pingMilliseconds": 1.48,
    "ok": 1
  },
  "exception": null,
  "tags": ["MongoDb", "db"],
  "entryType": "MongoDb"
}
```
