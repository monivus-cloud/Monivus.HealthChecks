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

## Example Entries (JSON)

Below are representative `entries.System` payloads from the `/health` endpoint. Field names reflect the implementation exactly.

Healthy (typical):

```json
{
  "status": "Healthy",
  "description": "System utilization within defined thresholds.",
  "duration": "00:00:00.0010000",
  "durationMs": 1,
  "data": {
    "ProcessName": "MyApp",
    "Is64BitProcess": true,
    "ProcessorCount": 8,
    "UptimeSeconds": 12345.67,
    "TotalProcessorTimeSeconds": 321.45,
    "MemoryUsagePercent": 42.1,
    "WorkingSetBytes": 152043520,
    "WorkingSetMegabytes": 145.32,
    "PrivateMemoryBytes": 210763776,
    "PagedMemoryBytes": 120258560,
    "PagedSystemMemoryBytes": 35651584,
    "NonPagedSystemMemoryBytes": 8388608,
    "EnvironmentWorkingSetBytes": 160432128,
    "HandleCount": 320,
    "ThreadCount": 34,
    "PriorityClass": "Normal",
    "GcTotalAllocatedBytes": 987654321,
    "GcHeapSizeBytes": 134217728,
    "GcFragmentedBytes": 4194304,
    "GcCommittedBytes": 167772160,
    "GcMemoryLoadBytes": 6871947673,
    "GcHighMemoryLoadThresholdBytes": 13743895347,
    "GcTotalAvailableMemoryBytes": 13743895347,
    "GcCollectionsGen0": 120,
    "GcCollectionsGen1": 60,
    "GcCollectionsGen2": 10,
    "GcPinnedObjectsCount": 2,
    "GcGeneration": 2,
    "GcPauseTimePercentage": 0.25
  },
  "exception": null,
  "tags": ["System"],
  "entryType": "System"
}
```

Degraded (memory usage exceeds threshold):

```json
{
  "status": "Degraded",
  "description": "Process memory usage 88.2% exceeds 85% threshold.",
  "duration": "00:00:00.0010000",
  "durationMs": 1,
  "data": {
    "ProcessName": "MyApp",
    "Is64BitProcess": true,
    "ProcessorCount": 8,
    "UptimeSeconds": 22345.11,
    "TotalProcessorTimeSeconds": 654.21,
    "MemoryUsagePercent": 88.2,
    "WorkingSetBytes": 3221225472,
    "WorkingSetMegabytes": 3072.0,
    "PrivateMemoryBytes": 3758096384,
    "PagedMemoryBytes": 3221225472,
    "PagedSystemMemoryBytes": 67108864,
    "NonPagedSystemMemoryBytes": 12582912,
    "EnvironmentWorkingSetBytes": 3305111552,
    "HandleCount": 420,
    "ThreadCount": 45,
    "PriorityClass": "Normal",
    "GcTotalAllocatedBytes": 1987654321,
    "GcHeapSizeBytes": 322122547,
    "GcFragmentedBytes": 10485760,
    "GcCommittedBytes": 402653184,
    "GcMemoryLoadBytes": 12111807744,
    "GcHighMemoryLoadThresholdBytes": 13743895347,
    "GcTotalAvailableMemoryBytes": 13743895347,
    "GcCollectionsGen0": 240,
    "GcCollectionsGen1": 120,
    "GcCollectionsGen2": 25,
    "GcPinnedObjectsCount": 3,
    "GcGeneration": 2,
    "GcPauseTimePercentage": 0.35
  },
  "exception": null,
  "tags": ["System"],
  "entryType": "System"
}
```
