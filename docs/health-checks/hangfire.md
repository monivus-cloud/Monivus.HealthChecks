---
icon: material/menu-right
---

# Hangfire Health Check

Monitors Hangfire server health and queue conditions via the Hangfire monitoring API.

Status rules:
- Healthy when storage is accessible and all configured thresholds are satisfied.
- Degraded when any threshold is violated:
  - `Failed` jobs > `MaxFailedJobs`
  - `Enqueued` jobs > `MaxEnqueuedJobs`
  - Registered servers < `MinServers`
- Unhealthy when storage/connection is inaccessible or an exception occurs.

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddHangfireEntry(name: "Hangfire");
```

Requires Hangfire `JobStorage` to be configured (e.g., SQL Server storage). The check resolves `JobStorage` from DI or uses `JobStorage.Current`.

## Configuration

Binding path: `Monivus:Hangfire`

### MinServers
Minimum number of registered servers required. Degraded when actual is less than this value.

```json
{
  "Monivus": { "Hangfire": { "MinServers": 1 } }
}
```

### MaxFailedJobs
Maximum allowed failed jobs. Degraded when the current `Failed` count exceeds this value.

```json
{
  "Monivus": { "Hangfire": { "MaxFailedJobs": 10 } }
}
```

### MaxEnqueuedJobs
Maximum allowed enqueued jobs. Degraded when the current `Enqueued` count exceeds this value.

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

Example interpretation: if there is at least 1 server, fewer than or equal to 10 failed jobs, and at most 1000 enqueued jobs, the check reports Healthy; otherwise Degraded. Storage connectivity issues result in Unhealthy.

## Example Entry (JSON)

`entries.Hangfire` inside the `/health` response when Healthy:

```json
{
  "status": "Healthy",
  "description": "Hangfire is healthy and running",
  "duration": "00:00:00.0030000",
  "durationMs": 3,
  "data": {
    "totalServers": 2,
    "succeededJobs": 1500,
    "failedJobs": 4,
    "processingJobs": 2,
    "scheduledJobs": 5,
    "enqueuedJobs": 20,
    "deletedJobs": 0,
    "recurringJobs": 12,
    "lastServerHeartbeat": "2024-11-07T21:30:00.123Z"
  },
  "exception": null,
  "tags": ["Hangfire"],
  "entryType": "Hangfire"
}
```
