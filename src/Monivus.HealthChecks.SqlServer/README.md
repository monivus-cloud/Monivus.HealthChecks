# Monivus.HealthChecks.SqlServer

SQL Server connectivity health checks using Microsoft.Data.SqlClient for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.SqlServer

## Install

```
dotnet add package Monivus.HealthChecks.SqlServer
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServerEntry(); // name: "SqlServer" by default
```

The extension binds options from configuration path `Monivus:SqlServer` and resolves the connection string by name from `ConnectionStrings`, or uses a raw connection string.

## Options (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Monivus": {
    "SqlServer": {
      "ConnectionStringOrName": "DefaultConnection",
      "CommandText": "SELECT 1",
      "CommandTimeout": 15
    }
  }
}
```

## Notes
- Registration name defaults to `SqlServer` and prepends the tag `SqlServer`.
- `CommandText` defaults to `SELECT 1`.

