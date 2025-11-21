# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Monivus.HealthChecks/Monivus.HealthChecks.csproj src/Monivus.HealthChecks/
COPY src/Monivus.HealthChecks.Exporter/Monivus.HealthChecks.Exporter.csproj src/Monivus.HealthChecks.Exporter/
COPY src/Monivus.Exporter/Monivus.Exporter.csproj src/Monivus.Exporter/

RUN dotnet restore src/Monivus.Exporter/Monivus.Exporter.csproj

COPY . .

RUN dotnet publish src/Monivus.Exporter/Monivus.Exporter.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Monivus.Exporter.dll"]
