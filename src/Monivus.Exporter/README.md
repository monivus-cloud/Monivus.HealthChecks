# Monivus.Exporter

Standalone agent that runs the same exporter as `Monivus.HealthChecks.Exporter`, but as its own host that you can install as a Windows/Linux service or run as a container. It periodically polls your application's health endpoint and pushes the report to Monivus Cloud.

- Targets: net8.0
- Runs as: console, Windows service, systemd service, or container

## Configuration

The agent uses the existing `Monivus:Exporter` options from the exporter library:

```json
{
  "Monivus": {
    "Exporter": {
      "Enabled": true,
      "ApplicationHealthCheckUrl": "http://localhost:8080/health",
      "MonivusCloudUrl": "https://cloud.monivus.com/api",
      "ApiKey": "<apikey>",
      "CheckInterval": 1,
      "HttpTimeout": "00:00:30"
    }
  }
}
```

Environment variables follow the usual double-underscore mapping, e.g.:

```bash
Monivus__Exporter__ApplicationHealthCheckUrl=https://my-app/health
Monivus__Exporter__MonivusCloudUrl=https://cloud.monivus.com/api
Monivus__Exporter__ApiKey=...
```

## Run locally

```bash
dotnet run --project src/Monivus.Exporter
```

## Windows service

1. Publish the app:
   ```powershell
   dotnet publish src/Monivus.Exporter/Monivus.Exporter.csproj -c Release -o ./artifacts/exporter
   ```
2. Install the service (requires admin):
   ```powershell
   New-Service -Name "MonivusExporter" `
     -BinaryPathName "\"$((Get-Item './artifacts/exporter/Monivus.Exporter.exe').FullName)\"" `
     -DisplayName "Monivus Exporter" `
     -StartupType Automatic
   Start-Service MonivusExporter
   ```
3. Configure via `appsettings.json` in the publish folder or environment variables.

## Linux systemd service

1. Publish and copy to the host (e.g., `/opt/monivus-exporter`).
2. Create `/etc/systemd/system/monivus-exporter.service`:
   ```ini
   [Unit]
   Description=Monivus Exporter
   After=network.target

[Service]
WorkingDirectory=/opt/monivus-exporter
ExecStart=/usr/bin/dotnet /opt/monivus-exporter/Monivus.Exporter.dll
   Environment=DOTNET_ENVIRONMENT=Production
   Environment=Monivus__Exporter__ApplicationHealthCheckUrl=https://my-app/health
   Environment=Monivus__Exporter__MonivusCloudUrl=https://cloud.monivus.com/api
   Restart=always
   RestartSec=5

   [Install]
   WantedBy=multi-user.target
   ```
3. Enable and start:
   ```bash
sudo systemctl daemon-reload
sudo systemctl enable monivus-exporter
sudo systemctl start monivus-exporter
   ```

## Docker

Build locally:
```bash
docker build -f src/Monivus.Exporter/Dockerfile -t ghcr.io/<owner>/monivus-exporter:local .
```

Run:
```bash
docker run --rm \
  -e Monivus__Exporter__ApplicationHealthCheckUrl=https://my-app/health \
  -e Monivus__Exporter__MonivusCloudUrl=https://cloud.monivus.com/api \
  -e Monivus__Exporter__ApiKey=... \
  ghcr.io/<owner>/monivus-exporter:local
```

To push to GHCR:
```bash
echo $GITHUB_TOKEN | docker login ghcr.io -u <owner> --password-stdin
docker push ghcr.io/<owner>/monivus-exporter:<tag>
```

A GitHub Actions workflow (`exporter-container.yml`) is included to build and push to GHCR either on demand or when tagging with `exporter-v*`.
