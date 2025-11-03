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
        private int _unauthorizedCount = 0;

        private sealed class UnauthorizedThresholdExceededException : Exception
        {
            public UnauthorizedThresholdExceededException(string message) : base(message) { }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monivus health exporter started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var options = _optionsMonitor.CurrentValue;
                    options.Normalize();

                    if (!options.Enabled)
                    {
                        _logger.LogDebug("Monivus exporter disabled; skipping cycle.");
                        await DelayAsync(options.CheckInterval, stoppingToken);
                        continue;
                    }

                    if (!TryBuildHealthUri(options, out var healthUri, out var healthError))
                    {
                        _logger.LogWarning("Health endpoint configuration invalid: {Error}", healthError);
                        await DelayAsync(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }

                    if (!TryBuildCentralUri(options, out var centralUri, out var centralError))
                    {
                        _logger.LogWarning("Central endpoint configuration invalid: {Error}", centralError);
                        await DelayAsync(TimeSpan.FromMinutes(1), stoppingToken);
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
                    catch (UnauthorizedThresholdExceededException ex)
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

                    await DelayAsync(options.CheckInterval, stoppingToken);
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
                if (string.Equals(options.ApiKeyHeaderName, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(options.ApiKeyScheme, options.ApiKey);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(options.ApiKeyHeaderName, options.ApiKey);
                }
            }

            var json = JsonSerializer.Serialize(report, SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.SendAsync(request, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _unauthorizedCount++;
                        _logger.LogWarning(
                            "Exporter received unauthorized (401) from {Endpoint}. Count: {Count}/20",
                            destination, _unauthorizedCount);
                        if (_unauthorizedCount >= 20)
                        {
                            throw new UnauthorizedThresholdExceededException(
                                "Exceeded 20 unauthorized responses while exporting health data.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Exporter received status {StatusCode} from {Endpoint}", (int)response.StatusCode, destination);
                    }
                }
                else
                {
                    if (_unauthorizedCount != 0)
                    {
                        _unauthorizedCount = 0;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not send data to central endpoint {Endpoint}", destination);
            }
        }

        private static bool TryBuildHealthUri(MonivusExporterOptions options, [NotNullWhen(true)] out Uri? uri, out string? error)
        {
            if (Uri.TryCreate(options.HealthCheckEndpoint, UriKind.Absolute, out var absolute))
            {
                uri = absolute;
                error = null;
                return true;
            }

            if (string.IsNullOrWhiteSpace(options.TargetApplicationUrl))
            {
                uri = null;
                error = "TargetApplicationUrl must be provided when HealthCheckEndpoint is relative.";
                return false;
            }

            if (!Uri.TryCreate(options.TargetApplicationUrl, UriKind.Absolute, out var baseUri))
            {
                uri = null;
                error = "TargetApplicationUrl must be a valid absolute URI.";
                return false;
            }

            if (!Uri.TryCreate(baseUri, options.HealthCheckEndpoint, out var combined))
            {
                uri = null;
                error = "HealthCheckEndpoint could not be combined with TargetApplicationUrl.";
                return false;
            }

            uri = combined;
            error = null;
            return true;
        }

        private static bool TryBuildCentralUri(MonivusExporterOptions options, [NotNullWhen(true)] out Uri? uri, out string? error)
        {
            if (string.IsNullOrWhiteSpace(options.CentralAppEndpoint))
            {
                uri = null;
                error = "CentralAppEndpoint configuration is required.";
                return false;
            }

            if (!Uri.TryCreate(options.CentralAppEndpoint, UriKind.Absolute, out var absolute))
            {
                uri = null;
                error = "CentralAppEndpoint must be a valid absolute URI.";
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
