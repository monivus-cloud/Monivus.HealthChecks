---
icon: material/menu-right
---

# Oracle Health Check

Executes a lightweight scalar query to verify connectivity and responsiveness to an Oracle database using Oracle.ManagedDataAccess.

Status rules:

- Healthy when the connection can be opened and the test command returns a non-null scalar result.
- Unhealthy when connection open, command execution, or the test query result fails.
- This check does not emit Degraded; prefer external SLAs or logging for latency thresholds.

## Install

```bash
dotnet add package Monivus.HealthChecks.Oracle
```

## Usage

```csharp
using Monivus.HealthChecks;

builder.Services.AddHealthChecks()
    .AddOracleEntry();
```

Configuration binding path: `Monivus:Oracle`

## Configuration

### ConnectionStringOrName
Gets or sets the Oracle connection string or the name of a connection string.

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=myuser;Password=mypassword;Data Source=localhost/FREEPDB1"
  },
  "Monivus": {
    "Oracle": { "ConnectionStringOrName": "OracleDb" }
  }
}
```

### CommandText
Gets or sets the SQL command text to execute. Defaults to `SELECT 1 FROM DUAL` and should remain extremely cheap.

```json
{
  "Monivus": { "Oracle": { "CommandText": "SELECT 1 FROM DUAL" } }
}
```

### CommandTimeout
Gets or sets the command timeout in seconds. When exceeded, the check returns Unhealthy with the timeout exception.

```json
{
  "Monivus": { "Oracle": { "CommandTimeout": 5 } }
}
```

Example interpretation: if the connection opens and the query returns promptly, the check reports Healthy. Any connection failure, timeout, or unexpected result reports Unhealthy.

## Example Entry (JSON)

`entries.Oracle` inside the `/health` response when Healthy:

```json
{
  "status": "Healthy",
  "description": "Oracle is healthy and running.",
  "duration": "00:00:00.0120000",
  "durationMs": 12,
  "data": {
    "connectionTimeout": 15,
    "state": 1,
    "commandTimeout": 30,
    "connectionOpenMilliseconds": 4.02,
    "queryDurationMilliseconds": 1.81
  },
  "exception": null,
  "tags": ["Oracle", "db"],
  "entryType": "Oracle"
}
```

