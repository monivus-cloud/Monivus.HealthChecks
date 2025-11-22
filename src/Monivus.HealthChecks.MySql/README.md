# Monivus.HealthChecks.MySql

MySQL connectivity health checks using MySqlConnector for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.MySql

## Install

```
dotnet add package Monivus.HealthChecks.MySql
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddMySqlEntry(); // name: "MySql" by default
```

## Options (appsettings.json)

```json
{
  "ConnectionStrings": {
    "MySqlDb": "Server=localhost;Port=3306;Database=mydb;Uid=myuser;Pwd=mypassword;"
  },
  "Monivus": {
    "MySql": {
      "ConnectionStringOrName": "MySqlDb",
      "CommandText": "SELECT 1",
      "CommandTimeout": 30
    }
  }
}
```

## Notes
- Registration name defaults to `MySql` and prepends the tag `MySql`.
- Connection string can be provided directly or resolved from `ConnectionStrings` by name.
- Command text defaults to `SELECT 1` but can be customized per environment.
