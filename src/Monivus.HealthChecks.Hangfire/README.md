# Monivus.HealthChecks.Hangfire

Hangfire storage and queue health checks for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.Hangfire

## Install

```
dotnet add package Monivus.HealthChecks.Hangfire
```

## Usage

```csharp
// Ensure Hangfire JobStorage is configured (e.g., GlobalConfiguration.Configuration.UseSqlServerStorage(...))

builder.Services.AddHealthChecks()
    .AddHangfireEntry(); // name: "Hangfire" by default
```

If no `JobStorage` is found in DI, the health check falls back to `JobStorage.Current`.

## Options (appsettings.json)

```json
{
  "Monivus": {
    "Hangfire": {
      "MinServers": 1,
      "MaxFailedJobs": 10,
      "MaxEnqueuedJobs": 1000
    }
  }
}
```

## Notes
- Registration name defaults to `Hangfire` and prepends the tag `Hangfire`.
- Throws if Hangfire `JobStorage` cannot be resolved.

