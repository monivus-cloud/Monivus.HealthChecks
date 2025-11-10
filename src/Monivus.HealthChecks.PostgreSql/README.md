# Monivus.HealthChecks.PostgreSql

PostgreSQL connectivity health checks using Npgsql for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.PostgreSql

## Install

```
dotnet add package Monivus.HealthChecks.PostgreSql
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddPostgreSqlEntry(); // name: "PostgreSql" by default
```

## Options (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Pg": "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword"
  },
  "Monivus": {
    "PostgreSql": {
      "ConnectionStringOrName": "Pg",
      "CommandText": "SELECT 1",
      "CommandTimeout": 30
    }
  }
}
```

## Notes
- Registration name defaults to `PostgreSql` and prepends the tag `PostgreSql`.
- Connection string can be provided directly or via `ConnectionStrings` by name.
