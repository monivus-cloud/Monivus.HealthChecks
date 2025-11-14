> # Beta version is live!

# Monivus.HealthChecks

Lightweight health checks for ASP.NET Core with a clean JSON response and optional remote aggregation.

What you’ll find here:
- Quick start and examples below

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register health checks you need
builder.Services.AddHealthChecks()
    .AddSystemEntry()
    .AddSqlServerEntry()
    .AddRedisEntry()
    .AddUrlEntry("Google");

var app = builder.Build();

// Expose default JSON endpoint at /health
app.UseMonivusHealthChecks("/health");

app.Run();
```
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

## Documentation

Full API details and advanced scenarios live at https://monivus-cloud.github.io/Monivus.HealthChecks/.


## Configuration Hints

You can configure most options via `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;"
  },
  "Monivus": {
    "SqlServer": {
      "ConnectionStringOrName": "DefaultConnection"
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

## Response Examples

Example response from a local `/health` endpoint:

```json
{
  "status": "Healthy",
  "timestamp": "2024-11-07T21:35:42.512Z",
  "duration": "00:00:00.0230000",
  "durationMs": 23,
  "traceId": "0HMG4H6P9A2Q1:00000001",
  "entries": {
    "SqlServer": {
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
    },
    "Redis": {
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
    },
  }
}
```

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

## Conventions

- Property names are camelCase; `status` values are strings (e.g., "Healthy").
- `duration` is an ISO-like TimeSpan string; `durationMs` is numeric and rounded.
- `entries` are keyed by registration name; aggregated remotes prefix entries with `{remoteName}|` and include a summary entry per remote.

## Available Health Checks

| Check | Entry Method | Description | NuGet |
| --- | --- | --- | --- |
| Hangfire | `AddHangfireEntry()` | Inspects Hangfire servers, failed jobs, and queue depth to catch background job issues early. | [![Monivus.HealthChecks.Hangfire NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Hangfire.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Hangfire/) |
| Oracle | `AddOracleEntry()` | Executes a lightweight query to confirm Oracle database connectivity and health. | [![Monivus.HealthChecks.Oracle NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Oracle.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Oracle/) |
| PostgreSql | `AddPostgreSqlEntry()` | Validates PostgreSQL connectivity using the configured connection string. | [![Monivus.HealthChecks.PostgreSql NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.PostgreSql.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.PostgreSql/) |
| Redis | `AddRedisEntry()` | Executes a Redis ping to ensure the cache node is reachable and responsive. | [![Monivus.HealthChecks.Redis NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Redis.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Redis/) |
| SqlServer | `AddSqlServerEntry()` | Runs a lightweight ping/query to verify SQL Server connectivity. | [![Monivus.HealthChecks.SqlServer NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.SqlServer.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.SqlServer/) |
| System | `AddSystemEntry()` | Validates CPU, memory, and other host-level metrics stay under configured thresholds. | [![Monivus.HealthChecks.System NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.System.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.System/) |
| Url | `AddUrlEntry()` | Calls arbitrary HTTP/S endpoints and tracks latency or failures. | [![Monivus.HealthChecks.Url NuGet](https://img.shields.io/nuget/v/Monivus.HealthChecks.Url.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks.Url/) |

## NuGet

[![NuGet Version](https://img.shields.io/nuget/v/Monivus.HealthChecks.svg?logo=nuget)](https://www.nuget.org/packages/Monivus.HealthChecks/)

Install via the NuGet Gallery: https://www.nuget.org/packages?q=Monivus.HealthChecks
