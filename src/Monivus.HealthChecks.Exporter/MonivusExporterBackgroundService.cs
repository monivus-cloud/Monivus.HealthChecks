using System.Net;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monivus.HealthChecks.Exporter
{
    /// <summary>
    /// Background service that periodically exports health check data to a central Monivus Cloud endpoint.
    /// </summary>
    /// <param name="httpClientFactory"></param>
    /// <param name="logger"></param>
    /// <param name="optionsMonitor"></param>
    public class MonivusExporterBackgroundService(
        IHttpClientFactory httpClientFactory,
        ILogger<MonivusExporterBackgroundService> logger,
        IOptionsMonitor<MonivusExporterOptions> optionsMonitor) : BackgroundService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<MonivusExporterBackgroundService> _logger = logger;
        private readonly IOptionsMonitor<MonivusExporterOptions> _optionsMonitor = optionsMonitor;
        private int _failedCount = 0;

        private sealed class FailedThresholdExceededException(string message) : Exception(message)
        {
        }

        /// <summary>
        /// Main execution loop of the background service.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monivus health exporter started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var options = _optionsMonitor.CurrentValue;
                    var interval = TimeSpan.FromMinutes(options.CheckInterval);

                    if (!options.Enabled)
                    {
                        _logger.LogDebug("Monivus exporter disabled; skipping cycle.");
                        await DelayAsync(interval, stoppingToken);
                        continue;
                    }

                    if (!TryBuildUri(options.ApplicationHealthCheckUrl, out var healthUri, out var healthError))
                    {
                        _logger.LogWarning("ApplicationHealthCheckUrl configuration invalid: {Error}", healthError);
                        await DelayAsync(interval, stoppingToken);
                        continue;
                    }

                    if (!TryBuildUri(options.MonivusCloudUrl, out var centralUri, out var centralError))
                    {
                        _logger.LogWarning("MonivusCloudUrl configuration invalid: {Error}", centralError);
                        await DelayAsync(interval, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var report = await FetchHealthDataAsync(healthUri, options, stoppingToken);
                        if (report != null)
                        {
                            await SendReportAsync(centralUri, report, options, stoppingToken);
                        }
                    }
                    catch (FailedThresholdExceededException ex)
                    {
                        _logger.LogError(ex, "Stopping exporter due to repeated unauthorized responses.");
                        break;
                    }
                    catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected exporter failure.");
                    }

                    await DelayAsync(interval, stoppingToken);
                }
            }
            finally
            {
                _logger.LogInformation("Monivus health exporter stopped.");
            }
        }

        private async Task<HealthCheckReport?> FetchHealthDataAsync(Uri endpoint, MonivusExporterOptions options, CancellationToken stoppingToken)
        {
            using var client = _httpClientFactory.CreateClient(nameof(MonivusExporterBackgroundService));
            client.Timeout = options.HttpTimeout;

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, stoppingToken);

                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogWarning("Health endpoint {Endpoint} returned {StatusCode}", endpoint, (int)response.StatusCode);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(stoppingToken);
                try
                {
                    var report = await JsonSerializer.DeserializeAsync<HealthCheckReport>(stream, SerializerOptions, stoppingToken);
                    if (report == null)
                    {
                        _logger.LogWarning("Health endpoint {Endpoint} returned an empty payload.", endpoint);
                    }
                    else
                    {
                        _logger.LogDebug("Health status {Status}", report.Status);
                    }

                    return report;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON payload received from {Endpoint}", endpoint);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach health endpoint {Endpoint}", endpoint);
                return null;
            }
        }

        private async Task SendReportAsync(Uri destination, HealthCheckReport report, MonivusExporterOptions options, CancellationToken stoppingToken)
        {
            using var client = _httpClientFactory.CreateClient($"{nameof(MonivusExporterBackgroundService)}-central");
            client.Timeout = options.HttpTimeout;

            using var request = new HttpRequestMessage(HttpMethod.Post, destination);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", options.ApiKey);
            }

            var json = JsonSerializer.Serialize(report, SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    _failedCount++;
                    _logger.LogWarning(
                        "Exporter received failed {StatusCode} from {Endpoint}. Count: {Count}/20",
                        response.StatusCode, destination, _failedCount);

                    if (_failedCount >= 20)
                    {
                        throw new FailedThresholdExceededException(
                            "Exceeded 20 failed responses while exporting health data.");
                    }
                }
                else
                {
                    if (_failedCount != 0)
                    {
                        _failedCount = 0;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not send data to central endpoint {Endpoint}", destination);
            }
        }

        private static bool TryBuildUri(string url, [NotNullWhen(true)] out Uri? uri, out string? error)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                uri = null;
                error = "MonivusCloudUrl configuration is required.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var absolute))
            {
                uri = null;
                error = "MonivusCloudUrl must be a valid absolute URI.";
                return false;
            }

            uri = absolute;
            error = null;
            return true;
        }

        private static Task DelayAsync(TimeSpan interval, CancellationToken stoppingToken)
        {
            if (interval <= TimeSpan.Zero)
            {
                interval = TimeSpan.FromMinutes(1);
            }

            return Task.Delay(interval, stoppingToken);
        }
    }
}
