# Monivus.HealthChecks.Redis

Redis connectivity health checks using StackExchange.Redis for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.Redis

## Install

```
dotnet add package Monivus.HealthChecks.Redis
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddRedisEntry(); // name: "Redis" by default
```

Behavior:
- If `Monivus:Redis:ConnectionString` is set, a new `ConnectionMultiplexer` is created.
- Otherwise, an `IConnectionMultiplexer` is resolved from DI (register one if you prefer shared connection).

## Options (appsettings.json)

```json
{
  "Monivus": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "SlowPingThresholdMilliseconds": 200
    }
  }
}
```

## Notes
- Registration name defaults to `Redis` and prepends the tag `Redis`.

