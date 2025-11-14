# Monivus.HealthChecks.Oracle

Oracle database connectivity health checks using Oracle.ManagedDataAccess for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.Oracle

## Install

```
dotnet add package Monivus.HealthChecks.Oracle
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddOracleEntry(); // name: "Oracle" by default
```

## Options (appsettings.json)

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=myuser;Password=mypassword;Data Source=localhost/FREEPDB1"
  },
  "Monivus": {
    "Oracle": {
      "ConnectionStringOrName": "OracleDb",
      "CommandText": "SELECT 1 FROM DUAL",
      "CommandTimeout": 30
    }
  }
}
```

## Notes
- Registration name defaults to `Oracle` and prepends the tag `Oracle`.
- Connection string can be provided directly or resolved from `ConnectionStrings` by name.
- Command text defaults to `SELECT 1 FROM DUAL` but can be customized per environment.

