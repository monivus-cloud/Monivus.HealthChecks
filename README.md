
# Monivus.HealthChecks

Real-time Health Checks with Deep Insight

Provides **rich JSON responses**, **deep diagnostics**, and **optional remote aggregation** for Monivus Cloud or your own dashboards.


## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register health checks you need
builder.Services.AddHealthChecks()
    .AddSystemEntry()
    .AddSqlServerEntry()
    .AddRedisEntry();

var app = builder.Build();

// Rich, structured JSON output (metrics, durations, exception details, tags)
app.UseMonivusHealthChecks("/health");

app.Run();
```

## Sample Aspire project

Want to see the health checks in action? There is a sample directory under `/sample` with an Aspire project you can easily run and test. From the root:

```bash
dotnet run --project sample/Monivus.AppHost/Monivus.AppHost.csproj
```

It spins up the sample services and exposes the Monivus health endpoints so you can hit them locally.

---

## Default ASP.NET HealthChecks vs Monivus HealthChecks

ASP.NET Core ships with the built-in middleware:

```csharp
app.UseHealthChecks("/healthz");
```

### Built-in HealthChecks Output (Microsoft)

- Does **not** return JSON.
- Response body is **plain text only** → `"Healthy"`, `"Degraded"`, `"Unhealthy"`.
- No entry-level details.
- No durations.
- No metrics.
- No structured schema.
- Not suitable for dashboards or aggregators.

Example output:

```
Healthy
```

---

## Monivus HealthChecks Output

When using:

```csharp
app.UseMonivusHealthChecks("/health");
```

You get a **structured, detailed JSON response**:

- Per-entry status (SQL, Redis, URL, Hangfire, custom checks)
- Duration per check (ms)
- Error messages + stack traces
- System metrics (CPU, memory, etc.)
- Tags & metadata for Monivus Cloud aggregation
- Dashboard-friendly uniform schema

Example output:

```json
{
  "status": "Degraded",
  "timestamp": "2025-11-29T09:55:19.7662916Z",
  "duration": "00:00:00.4225265",
  "durationMs": 422.526,
  "traceId": "0HNHF5LGU6R9V:00000017",
  "entries": {
    "SqlServer": {
      {
      "status": "Healthy",
      "description": "SqlServer is healthy and running.",
      "duration": "00:00:00.0100000",
      "durationMs": 10,
      "data": {
        "connectionTimeout": 15,
        "state": 1,
        "commandTimeout": 30,
        "connectionOpenMilliseconds": 3.21,
        "queryDurationMilliseconds": 1.45
      },
      "exception": null,
      "tags": ["SqlServer", "db"],
      "entryType": "SqlServer"
    }
    },
    "Redis": {
      "status": "Healthy",
      "description": "Redis is healthy and responsive.",
      "duration": "00:00:00.0679099",
      "durationMs": 67.91,
      "data": {
        "IsConnected": true,
        "ServerVersion": "8.2.1",
        "ServerType": "Standalone",
        "PingMilliseconds": 37.53,
        "DatabaseSize": 0,
        "LastSaveUtc": "2025-11-29T09:54:56.0000000Z",
        "UsedMemoryMb": 0.96,
        "UsedMemoryRssMb": 12.19,
        "TotalSystemMemoryMb": 15821.16,
        "MemoryUsagePercent": 0.01,
        "ConnectedClients": 4,
        "BlockedClients": 0,
        "OpsPerSecond": 0,
        "UptimeSeconds": 23,
        "MemoryFragmentationRatio": 12.73,
        "KeyspaceHits": 0,
        "KeyspaceMisses": 2,
        "KeyspaceHitRatePercent": 0
      },
      "exception": null,
      "tags": [
        "Redis"
      ],
      "entryType": "Redis"
    },
    "System": {
      "status": "Degraded",
      "description": "Process memory usage 90% exceeds 90% threshold.",
      "duration": "00:00:00.0378217",
      "durationMs": 37.822,
      "data": {
        "ProcessName": "Monivus.UI",
        "Is64BitProcess": true,
        "ProcessorCount": 20,
        "UptimeSeconds": 16.99,
        "TotalProcessorTimeSeconds": 4.14,
        "MemoryUsagePercent": 90,
        "WorkingSetBytes": 199917568,
        "WorkingSetMegabytes": 190.66,
        "PrivateMemoryBytes": 78909440,
        "PagedMemoryBytes": 78909440,
        "PagedSystemMemoryBytes": 566240,
        "NonPagedSystemMemoryBytes": 150880,
        "EnvironmentWorkingSetBytes": 199917568,
        "HandleCount": 1134,
        "ThreadCount": 70,
        "PriorityClass": "Normal",
        "GcTotalAllocatedBytes": 36926464,
        "GcHeapSizeBytes": 21246296,
        "GcFragmentedBytes": 5051848,
        "GcCommittedBytes": 27648000,
        "GcMemoryLoadBytes": 30599545651,
        "GcHighMemoryLoadThresholdBytes": 30599545651,
        "GcTotalAvailableMemoryBytes": 33999495168,
        "GcCollectionsGen0": 4,
        "GcCollectionsGen1": 2,
        "GcCollectionsGen2": 2,
        "GcPinnedObjectsCount": 9,
        "GcGeneration": 0,
        "GcPauseTimePercentage": 0.14
      },
      "exception": null,
      "tags": [
        "System"
      ],
      "entryType": "System"
    }
  }
}
```

---

## Summary

| Feature | `UseHealthChecks` | `UseMonivusHealthChecks` |
|--------|-------------------|---------------------------|
| JSON response | ✖ (plain text only) | ✔ |
| Overall status | ✔ | ✔ |
| Per-entry breakdown | ✖ | ✔ |
| Durations | ✖ | ✔ |
| Exception details | ✖ | ✔ |
| Additional metrics | ✖ | ✔ |
| Aggregation for Monivus Cloud | ✖ | ✔ |
| Developer debug friendliness | Low | High |

---

## Aggregated Checks

Optionally expose an aggregated endpoint that merges local + remote services
```csharp
// instead of UseMonivusHealthChecks
app.UseMonivusAggregatedHealthChecks(options =>
{
    options.AddEndpoint("https://service-a.example.com/health", name: "service-a");
    options.AddEndpoint("https://service-b.example.com/health", name: "service-b");
}, path: "/healthz");

app.Run();
```

<img width="756" height="447" alt="image" src="https://github.com/user-attachments/assets/70006845-47e4-4bab-8a89-0a1d0a30407a" />

Example response from an aggregated endpoint (e.g., `/healthz`) that merges local checks plus two remotes:

```json
{
  "status": "Healthy",
  "timestamp": "2024-11-07T21:36:12.101Z",
  "duration": "00:00:00.0450000",
  "durationMs": 45,
  "traceId": "0HMG4H6P9A2Q2:00000002",
  "entries": {
    "System": { "status": "Healthy", "description": "System utilization within defined thresholds.", "duration": "00:00:00.0010000", "durationMs": 1, "data": null, "exception": null, "tags": ["System"], "entryType": "System" },

    "service-a": { "status": "Healthy", "description": null, "duration": "00:00:00.0200000", "durationMs": 20, "data": { "statusCode": 200 }, "exception": null, "tags": [], "entryType": "Service" },
    "service-a|System": { "status": "Healthy", "description": "System utilization within defined thresholds.", "duration": "00:00:00.0010000", "durationMs": 1, "data": null, "exception": null, "tags": ["System"], "entryType": "System" },
    "service-a|SqlServer": { "status": "Healthy", "description": "SqlServer is healthy and running.", "duration": "00:00:00.0100000", "durationMs": 10, "data": { "connectionOpenMilliseconds": 3.1, "queryDurationMilliseconds": 1.2 }, "exception": null, "tags": ["SqlServer"], "entryType": "SqlServer" },

    "service-b": { "status": "Unhealthy", "description": "Request timed out.", "duration": "00:00:00.5000000", "durationMs": 500, "data": { "statusCode": 504 }, "exception": "System.Threading.Tasks.TaskCanceledException", "tags": [], "entryType": "Service" }
  }
}
```

## Documentation

Full API details and advanced scenarios live at https://docs.monivus.com

## Exporter

Prefer to run the exporter as a standalone service or container? [See](src/Monivus.Exporter/README.md); it hosts the same exporter logic and can run as a Windows service, systemd unit, or Docker container pointing at your app's `/health` endpoint.

## Configuration Hints

You can configure most options via `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;",
    "MySql": "Server=localhost;Port=3306;Database=MyDb;Uid=myuser;Pwd=mypassword;"
  },
  "Monivus": {
    "SqlServer": {
      "ConnectionStringOrName": "DefaultConnection"
    },
    "MySql": {
      "ConnectionStringOrName": "MySql"
    },
    "Redis": {
      "ConnectionString": "localhost:6379"
    },
    "Urls": {
      "Google": { "Url": "https://www.google.com" }
    },
    "System": {
      "MemoryUsageThresholdPercent": 85
    },
    "Hangfire": {
      "MinServers": 1,
      "MaxFailedJobs": 10,
      "MaxEnqueuedJobs": 1000
    }
  }
}
```

## Conventions

- Property names are camelCase; `status` values are strings (e.g., "Healthy").
- `duration` is an ISO-like TimeSpan string; `durationMs` is numeric and rounded.
- `entries` are keyed by registration name; aggregated remotes prefix entries with `{remoteName}|` and include a summary entry per remote.

## Available Health Checks

| Check | Entry Method | Description | NuGet |
| --- | --- | --- | --- |
| Hangfire | `AddHangfireEntry()` | Inspects Hangfire servers, failed jobs, and queue depth to catch background job issues early. | [![Monivus.HealthChecks.Hangfire NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Hangfire.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Hangfire/) |
| MongoDB | `AddMongDbEntry()` | MongoDB connectivity health checks using MongoDB.Driver for Monivus. | [![Monivus.HealthChecks.MongDb NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.MongoDb.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.MongoDb/) |
| MySQL | `AddMySqlEntry()` | Executes a lightweight query to confirm MySQL database connectivity and health. | [![Monivus.HealthChecks.MySql NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.MySql.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.MySql/) |
| Oracle | `AddOracleEntry()` | Executes a lightweight query to confirm Oracle database connectivity and health. | [![Monivus.HealthChecks.Oracle NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Oracle.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Oracle/) |
| PostgreSql | `AddPostgreSqlEntry()` | Validates PostgreSQL connectivity using the configured connection string. | [![Monivus.HealthChecks.PostgreSql NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.PostgreSql.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.PostgreSql/) |
| Redis | `AddRedisEntry()` | Executes a Redis ping to ensure the cache node is reachable and responsive. | [![Monivus.HealthChecks.Redis NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Redis.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Redis/) |
| SqlServer | `AddSqlServerEntry()` | Runs a lightweight ping/query to verify SQL Server connectivity. | [![Monivus.HealthChecks.SqlServer NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.SqlServer.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.SqlServer/) |
| System | `AddSystemEntry()` | Validates CPU, memory, and other host-level metrics stay under configured thresholds. | [![Monivus.HealthChecks.System NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.System.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.System/) |
| Url | `AddUrlEntry()` | Calls arbitrary HTTP/S endpoints and tracks latency or failures. | [![Monivus.HealthChecks.Url NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Url.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Url/) |

## NuGet

[![NuGet Version](https://img.shields.io/nuget/v/Monivus.HealthChecks.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks/)

Install via the NuGet Gallery: https://www.nuget.org/packages?q=Monivus.HealthChecks
