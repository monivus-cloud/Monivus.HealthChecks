
# Monivus Exporter

Exports your app's health check report to a central endpoint on a schedule.

What it does:

- Periodically GETs your local health endpoint (JSON).
- POSTs the same JSON to your Monivus Cloud URL (or any you have).
- Uses a lightweight hosted background service with configurable interval and timeout.

## Install

```bash
dotnet add package Monivus.HealthChecks.Exporter
```

## Usage

```csharp
using Monivus.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Register exporter (binds Monivus:Exporter)
builder.Services.AddMonivusExporter(builder.Configuration);

// Typical health endpoint exposed for the exporter to read
var app = builder.Build();
app.UseMonivusHealthChecks("/health");
app.Run();
```

## Standalone host (service/container)

If you prefer not to wire the exporter inside your app, use the standalone host in `src/Monivus.Exporter`:

- Run locally: `dotnet run --project src/Monivus.Exporter`
- Windows service: publish then install `MonivusExporter` pointing to the published `Monivus.Exporter.exe`
- systemd: run `/usr/bin/dotnet /opt/monivus-exporter/Monivus.Exporter.dll`
- Docker: `docker build -f src/Monivus.Exporter/Dockerfile -t ghcr.io/<owner>/monivus-exporter:local .`
- ENV overrides in containers/services:
  - `Monivus__Exporter__ApplicationHealthCheckUrl`
  - `Monivus__Exporter__MonivusCloudUrl` (e.g., `https://cloud.monivus.com/api`)
  - `Monivus__Exporter__ApiKey`

## Configuration

Binding path: `Monivus:Exporter`

Keys:

- `Enabled` (bool, default true): Turns exporter on/off.
- `ApplicationHealthCheckUrl` (string, required): Absolute URL to your app's health endpoint (e.g., `https://api.example.com/health`).
- `MonivusCloudUrl` (string, required): Absolute URL to receive payloads (e.g., `https://cloud.monivus.com/api`).
- `ApiKey` (string, optional): Sent as `Authorization: ApiKey {value}`.
- `CheckInterval` (int, minutes, default 1): Interval between export cycles.
- `HttpTimeout` (TimeSpan, default 00:00:30): Timeout for GET and POST.

Example:

```json
{
  "Monivus": {
    "Exporter": {
      "Enabled": true,
      "ApplicationHealthCheckUrl": "https://your-app.example.com/health",
      "MonivusCloudUrl": "https://cloud.monivus.com/api",
      "ApiKey": "YOUR_API_KEY",
      "CheckInterval": 1,
      "HttpTimeout": "00:00:30"
    }
  }
}
```

## Behavior

- Schedule: runs every `CheckInterval` minutes; waits even when disabled.
- Fetch: GETs `ApplicationHealthCheckUrl` with `Accept: application/json`.
  - Accepts 2xx as success; logs warning and skips on non-2xx except 503 (Service Unavailable), which is tolerated.
- Send: POSTs to `MonivusCloudUrl` with `Content-Type: application/json`.
  - Adds `Authorization: ApiKey {ApiKey}` when configured.
  - On 401 responses, increments an internal counter and stops after 20 consecutive 401s.
- Serialization: Uses camelCase, enum strings, ignores nulls; payload shape matches the standard Monivus health response.

## Payload Example

The exporter posts a `HealthCheckReport` JSON identical to your `/health` response. See full samples in: `docs/index.md`.

Short example (truncated):

```json
{
  "status": "Healthy",
  "timestamp": "2024-11-07T21:35:42.512Z",
  "duration": "00:00:00.0230000",
  "durationMs": 23,
  "traceId": "0H...:0001",
  "entries": {
    "System": { "status": "Healthy", "durationMs": 1, "entryType": "System" },
    "SqlServer": { "status": "Healthy", "durationMs": 10, "entryType": "SqlServer" }
  }
}
```

## Troubleshooting

- 401 Unauthorized: verify `ApiKey`; exporter stops after 20 consecutive 401s.
- Invalid URLs: both URLs must be absolute; misconfigurations are logged and the cycle is skipped.
- Timeouts: adjust `HttpTimeout` if your health check or network is slow.
- Service Unavailable (503): considered a valid health read; payload still forwarded.
