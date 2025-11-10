---
icon: material/menu-right
---

# System Health Check

Monitors basic process/system health. Useful for a lightweight, always-on check.

Status rules:

- Healthy when utilization is within configured thresholds.
- Degraded when a configured threshold is exceeded.
- This check does not return Unhealthy by itself; only operational errors would do so.

## Install

```bash
dotnet add package Monivus.HealthChecks.System
```

## Usage

```csharp
using Monivus.HealthChecks;

builder.Services.AddHealthChecks()
    .AddSystemEntry(name: "System");
```

Then expose the endpoint:

```csharp
app.UseMonivusHealthChecks("/health");
```

## Configuration

App settings section: `Monivus:System`

### MemoryUsageThresholdPercent
Percentage of total available memory used by the current process that triggers a degraded state.

Behavior:

- Healthy when `MemoryUsageThresholdPercent` is not set, or the current usage is below the threshold.
- Degraded when current usage is greater than or equal to the threshold.

Notes:
- The percentage is computed using GC memory load versus total available memory and clamped to 0–100.

```json
{
  "Monivus": {
    "System": {
      "MemoryUsageThresholdPercent": 85
    }
  }
}
```

Example interpretation: if the process memory usage reaches 85% or higher of available memory, the check reports Degraded; otherwise Healthy.

## Example Entry (JSON)

A typical `entries.System` object inside the `/health` response:

```json
{
  "status": "Healthy",
  "description": "System utilization within defined thresholds.",
  "duration": "00:00:00.0010000",
  "durationMs": 1,
  "data": {
    "processName": "MyApp",
    "processorCount": 8,
    "uptimeSeconds": 12345.67,
    "memoryUsagePercent": 42.1,
    "workingSetMegabytes": 145.32
  },
  "exception": null,
  "tags": ["System"],
  "entryType": "System"
}
```
