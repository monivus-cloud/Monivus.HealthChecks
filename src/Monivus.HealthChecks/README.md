# Monivus.HealthChecks

Lightweight health checks for ASP.NET Core with a clean JSON response and optional aggregation of remote endpoints.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks

## Install

```
dotnet add package Monivus.HealthChecks
```

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddSystemEntry()
    .AddSqlServerEntry()
    .AddRedisEntry()
    .AddUrlEntry("Google", url: "https://www.google.com");

var app = builder.Build();

// Default JSON endpoint at /health
app.UseMonivusHealthChecks("/health");

// Optional aggregated endpoint merging local + remote
app.UseMonivusAggregatedHealthChecks(options =>
{
    options.AddEndpoint("https://service-a.example.com/health", name: "service-a");
    options.AddEndpoint("https://service-b.example.com/health", name: "service-b");
}, path: "/healthz");

app.Run();
```

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;"
  },
  "Monivus": {
    "SqlServer": {
      "ConnectionStringOrName": "DefaultConnection",
      "CommandText": "SELECT 1",
      "CommandTimeout": 15
    },
    "Redis": {
      "ConnectionString": "localhost:6379",
      "SlowPingThresholdMilliseconds": 200
    },
    "Urls": {
      "Google": { "Url": "https://www.google.com", "RequestTimeout": "00:00:05" }
    },
    "System": {
      "MemoryUsageThresholdPercent": 85
    }
  }
}
```

## Notes
- `UseMonivusHealthChecks` emits a compact JSON with overall status, timing, trace ID, and entries.
- Aggregation prefixes remote entries with `{remoteName}|` plus a per-remote summary entry.

