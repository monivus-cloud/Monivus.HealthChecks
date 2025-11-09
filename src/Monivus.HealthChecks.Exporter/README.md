# Monivus.HealthChecks.Exporter

Background service exporting Monivus health reports to a central endpoint.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.Exporter

## Install

```
dotnet add package Monivus.HealthChecks.Exporter
```

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMonivusExporter(builder.Configuration);

var app = builder.Build();
app.Run();
```

Exports by periodically fetching your app's health endpoint and posting to Monivus Cloud.

## Options (appsettings.json)

```json
{
  "Monivus": {
    "Exporter": {
      "Enabled": true,
      "ApplicationHealthCheckUrl": "https://localhost:5001/health",
      "MonivusCloudUrl": "https://cloud.monivus.example/api/health",
      "ApiKey": "<optional>",
      "CheckInterval": 1,
      "HttpTimeout": "00:00:30"
    }
  }
}
```

## Notes
- Requires your health endpoint to be accessible from this process.
- `Enabled` can be toggled at runtime via configuration reloads.

