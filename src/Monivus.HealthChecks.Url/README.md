# Monivus.HealthChecks.Url

HTTP/URL reachability health checks for Monivus.

- Targets: net8.0, net9.0
- NuGet: Monivus.HealthChecks.Url

## Install

```
dotnet add package Monivus.HealthChecks.Url
```

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddUrlEntry("Google", url: "https://www.google.com");
```

The registration name (e.g., `Google`) is also the configuration key under `Monivus:Urls:{name}`.

## Options (appsettings.json)

```json
{
  "Monivus": {
    "Urls": {
      "Google": {
        "Url": "https://www.google.com",
        "RequestTimeout": "00:00:05"
      }
    }
  }
}
```

## Notes
- Provide `url` parameter as a fallback when not present in configuration.
- `RequestTimeout` can be overridden by the registration `timeout` parameter.

