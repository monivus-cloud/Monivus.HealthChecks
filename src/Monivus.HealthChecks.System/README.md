# Monivus.HealthChecks.System

Process/system resource health checks (e.g., memory load) for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.System

## Install

```
dotnet add package Monivus.HealthChecks.System
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddSystemEntry(); // name: "System" by default
```

## Options (appsettings.json)

```json
{
  "Monivus": {
    "System": {
      "MemoryUsageThresholdPercent": 85
    }
  }
}
```

## Notes
- Registration name defaults to `System` and prepends the tag `System`.

