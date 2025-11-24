# Monivus.HealthChecks.MongoDb

MongoDB connectivity health checks using MongoDB.Driver for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.MongoDb

## Install

```
dotnet add package Monivus.HealthChecks.MongoDb
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddMongoDbEntry(); // name: "MongoDb" by default
```

## Options (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://myuser:mypassword@localhost:27017/?retryWrites=true&w=majority"
  },
  "Monivus": {
    "MongoDb": {
      "ConnectionStringOrName": "Mongo",
      "DatabaseName": "admin",
      "PingLatencyThresholdMs": 250
    }
  }
}
```

## Notes
- Registration name defaults to `MongoDb` and prepends the tag `MongoDb`.
- Connection string can be provided directly or resolved from `ConnectionStrings` by name.
- If you omit the connection string, ensure an `IMongoClient` is registered in DI.
- The health check issues a `ping` command against the configured database (defaults to `admin`). When `PingLatencyThresholdMs` is set, latency beyond the threshold returns Degraded.
