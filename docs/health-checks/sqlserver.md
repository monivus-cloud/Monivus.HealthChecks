---
icon: material/menu-right
---

# SQL Server Health Check

Executes a lightweight SQL command to verify connectivity and responsiveness.

Status rules:

- Healthy when the connection can be opened and the test command returns a non-null scalar result.
- Unhealthy when connection open, command execution, or the test result fails.
- This check does not report Degraded; consider using external SLAs or logs for latency thresholds.

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServerEntry(name: "SqlServer", tags: new[] { "db" });
```

Configuration binding path: `Monivus:SqlServer`

## Configuration

### ConnectionStringOrName
Gets or sets the SQL Server connection string or the name of a connection string.

```json
{
  "ConnectionStrings": { "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;" },
  "Monivus": { "SqlServer": { "ConnectionStringOrName": "DefaultConnection" } }
}
```

### CommandText
Gets or sets the SQL command text to be executed for the health check. Should be a very cheap scalar query (e.g., `SELECT 1`).

```json
{
  "Monivus": { "SqlServer": { "CommandText": "SELECT 1" } }
}
```

### CommandTimeout
Gets or sets the command timeout in seconds for the SQL command. If the command exceeds this timeout, the check returns Unhealthy with the corresponding exception.

```json
{
  "Monivus": { "SqlServer": { "CommandTimeout": 5 } }
}
```

Example interpretation: if connecting runs and `SELECT 1` returns promptly, the check reports Healthy. If connecting or executing fails or times out, the check reports Unhealthy.

## Example Entry (JSON)

`entries.SqlServer` inside the `/health` response when Healthy:

```json
{
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
}
```
