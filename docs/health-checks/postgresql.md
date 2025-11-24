---
icon: material/menu-right
---

# PostgreSQL Health Check

Executes a lightweight SQL command to verify connectivity and responsiveness to a PostgreSQL database via Npgsql.

Status rules:

- Healthy when the connection can be opened and the test command returns a non-null scalar result.
- Unhealthy when connection open, command execution, or the test result fails.
- This check does not report Degraded; consider using external SLAs or logs for latency thresholds.

## Install

```bash
dotnet add package Monivus.HealthChecks.PostgreSql
```

## Usage

```csharp
using Monivus.HealthChecks;

builder.Services.AddHealthChecks()
    .AddPostgreSqlEntry();
```

Configuration binding path: `Monivus:PostgreSql`

## Configuration

### ConnectionStringOrName
Gets or sets the PostgreSQL connection string or the name of a connection string.

```json
{
  "ConnectionStrings": {
    "Pg": "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword"
  },
  "Monivus": {
    "PostgreSql": { "ConnectionStringOrName": "Pg" }
  }
}
```

### CommandText
Gets or sets the SQL command text to be executed for the health check. Should be a very cheap scalar query (e.g., `SELECT 1`).

```json
{
  "Monivus": { "PostgreSql": { "CommandText": "SELECT 1" } }
}
```

### CommandTimeout
Gets or sets the command timeout in seconds for the SQL command. If the command exceeds this timeout, the check returns Unhealthy with the corresponding exception.

```json
{
  "Monivus": { "PostgreSql": { "CommandTimeout": 5 } }
}
```

Example interpretation: if connecting runs and `SELECT 1` returns promptly, the check reports Healthy. If connecting or executing fails or times out, the check reports Unhealthy.

## Example Entry (JSON)

`entries.PostgreSql` inside the `/health` response when Healthy:

```json
{
  "status": "Healthy",
  "description": "PostgreSQL is healthy and running.",
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
  "tags": ["PostgreSql", "db"],
  "entryType": "PostgreSql"
}
```

