---
icon: material/menu-right
---

# URL Health Check

Validates that an HTTP/HTTPS endpoint responds as expected.

Status rules:

- Healthy when the response status code is in the expected set (defaults to 2xx) and, if configured, the response time does not exceed the slow threshold.
- Degraded when the response status code is expected but the response time exceeds `SlowResponseThreshold`.
- Unhealthy when the response status code is not in the expected set, the request times out, or an exception occurs.

## Usage

```csharp
builder.Services.AddHealthChecks()
    .AddUrlEntry(name: "Google", url: "https://www.google.com");
```

You can also configure by name and omit `url`; the check reads `Monivus:Urls:{name}`.

## Configuration

Binding path: `Monivus:Urls:{name}`

### Url
Gets or sets the absolute HTTP/HTTPS URL to monitor.

```json
{
  "Monivus": { "Urls": { "Google": { "Url": "https://www.google.com" } } }
}
```

### Method
Gets or sets the HTTP method to use when making the request. Defaults to GET.

```json
{
  "Monivus": { "Urls": { "Google": { "Method": "GET" } } }
}
```

### RequestTimeout
Gets or sets the timeout duration for the HTTP request. This maps to `HttpClient.Timeout`. Use the `hh:mm:ss` TimeSpan format.

```json
{
  "Monivus": { "Urls": { "Google": { "RequestTimeout": "00:00:05" } } }
}
```

### ExpectedStatusCodes
Gets or sets the set of expected HTTP status codes that indicate a healthy response.

```json
{
  "Monivus": { "Urls": { "Google": { "ExpectedStatusCodes": [200, 204] } } }
}
```

### SlowResponseThreshold
Threshold duration for considering a response as slow (used to report Degraded). Use the `hh:mm:ss` TimeSpan format.

```json
{
  "Monivus": {
    "Urls": {
      "Google": {
        "Url": "https://www.google.com",
        "Method": "GET",
        "RequestTimeout": "00:00:05",
        "ExpectedStatusCodes": [200, 204],
        "SlowResponseThreshold": "00:00:02"
      }
    }
  }
}
```

Example interpretation: responses with 2xx are Healthy unless they take longer than 2 seconds, in which case the check reports Degraded. Non-expected status codes or timeouts are Unhealthy.

## Example Entry (JSON)

`entries.Google` inside the `/health` response when Healthy and quick:

```json
{
  "status": "Healthy",
  "description": null,
  "duration": "00:00:00.0060000",
  "durationMs": 6,
  "data": {
    "url": "https://www.google.com",
    "method": "GET",
    "statusCode": 200,
    "reasonPhrase": "OK"
  },
  "exception": null,
  "tags": ["Url"],
  "entryType": "Url"
}
```
