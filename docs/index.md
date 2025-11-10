# Monivus.HealthChecks

Lightweight health checks for ASP.NET Core with a clean JSON response and optional remote aggregation.

What you'll find here:
- Quick start and examples below

## Install Packages

Install the core package and only the checks you need:

```bash
dotnet add package Monivus.HealthChecks
dotnet add package Monivus.HealthChecks.System
dotnet add package Monivus.HealthChecks.SqlServer
dotnet add package Monivus.HealthChecks.Redis
dotnet add package Monivus.HealthChecks.Url
```

## Quick Start

=== "code"
	```csharp
	using Monivus.HealthChecks;

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

=== "response example"
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

## Aggregated Checks

Optionally expose an aggregated endpoint that merges local + remote services

=== "code"
	```csharp
	using Monivus.HealthChecks;

	// instead of UseMonivusHealthChecks
	app.UseMonivusAggregatedHealthChecks(options =>
	{
		options.AddEndpoint("https://service-a.example.com/health", name: "service-a");
		options.AddEndpoint("https://service-b.example.com/health", name: "service-b");
	}, path: "/healthz");

	app.Run();
	```
	
=== "response example"
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
Entries diagram example:
``` mermaid
graph LR
  A[Application] --> E1[Redis];
  A --> E2[URL];
  A --> SA[Service A]
  A --> SB[Service B]
  SA --> E3[Sql Server]
  SA --> E4[Hangfire]
  SB --> E5[Postgres]
```

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

## Conventions

- Property names are camelCase; `status` values are strings (e.g., "Healthy").
- `duration` is an ISO-like TimeSpan string; `durationMs` is numeric and rounded.
- `entries` are keyed by registration name; aggregated remotes prefix entries with `{remoteName}|` and include a summary entry per remote.
