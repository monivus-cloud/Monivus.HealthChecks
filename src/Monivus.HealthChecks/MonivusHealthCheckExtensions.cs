using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monivus.HealthChecks
{
    public static class MonivusHealthCheckExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static IApplicationBuilder UseMonivusHealthChecks(this IApplicationBuilder app, string path = "/health")
        {
            return app.UseHealthChecks(path, new HealthCheckOptions
            {
                ResponseWriter = WriteMonivusHealthResponse
            });
        }

        public static IApplicationBuilder UseMonivusAggregatedHealthChecks(
            this IApplicationBuilder app,
            Action<AggregatedHealthOptions> configure,
            string path = "/health")
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(configure);

            var options = new AggregatedHealthOptions();
            configure(options);
            options.Normalize();

            // Allow zero or more remote endpoints (if zero, behaves like default writer for local entries)

            return app.UseHealthChecks(path, new HealthCheckOptions
            {
                ResponseWriter = (ctx, report) => WriteAggregatedResponseAsync(ctx, report, options)
            });
        }

        private static Task WriteMonivusHealthResponse(HttpContext context, HealthReport report)
        {
            var entryResults = new Dictionary<string, HealthCheckEntry>(report.Entries.Count, StringComparer.OrdinalIgnoreCase);
            var totalEntryDurationMs = 0d;
            var maxEntryDurationMs = 0d;
            var healthyCount = 0;
            var degradedCount = 0;
            var unhealthyCount = 0;
            var unknownCount = 0;

            foreach (var entry in report.Entries)
            {
                var source = entry.Value;
                var responseEntry = new HealthCheckEntry
                {
                    Status = source.Status,
                    Description = source.Description,
                    Duration = source.Duration,
                    Data = source.Data?.ToDictionary(
                        d => d.Key,
                        d => d.Value is Exception ex ? ex.Message : d.Value),
                    Exception = source.Exception?.GetType().FullName,
                    Tags = source.Tags,
                    EntryType = InferEntryType(source.Tags)
                };

                entryResults[entry.Key] = responseEntry;

                var durationMs = responseEntry.Duration.TotalMilliseconds;
                totalEntryDurationMs += durationMs;
                if (durationMs > maxEntryDurationMs)
                {
                    maxEntryDurationMs = durationMs;
                }

                switch (source.Status)
                {
                    case HealthStatus.Healthy:
                        healthyCount++;
                        break;
                    case HealthStatus.Degraded:
                        degradedCount++;
                        break;
                    case HealthStatus.Unhealthy:
                        unhealthyCount++;
                        break;
                    default:
                        unknownCount++;
                        break;
                }
            }

            var response = new HealthCheckReport
            {
                Status = report.Status,
                Timestamp = DateTime.UtcNow,
                Duration = report.TotalDuration,
                TraceId = context.TraceIdentifier,
                Entries = entryResults
            };

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response, JsonOptions));
        }

        private static async Task WriteAggregatedResponseAsync(HttpContext context, HealthReport localReport, AggregatedHealthOptions options)
        {
            var mergedEntries = new Dictionary<string, HealthCheckEntry>(StringComparer.OrdinalIgnoreCase);

            // Gather local entries first
            foreach (var entry in localReport.Entries)
            {
                var source = entry.Value;
                mergedEntries[entry.Key] = new HealthCheckEntry
                {
                    Status = source.Status,
                    Description = source.Description,
                    Duration = source.Duration,
                    Data = source.Data?.ToDictionary(d => d.Key, d => d.Value is Exception ex ? ex.Message : d.Value),
                    Exception = source.Exception?.GetType().FullName,
                    Tags = source.Tags,
                    EntryType = InferEntryType(source.Tags)
                };
            }

            // Fetch all remote health endpoints concurrently and merge
            if (options.RemoteEndpoints.Count > 0)
            {
                var sp = context.RequestServices;
                var clientFactory = sp.GetService<IHttpClientFactory>();

                var tasks = options.RemoteEndpoints.Select(ep => FetchRemoteAsync(
                    clientFactory,
                    url: ep.Url,
                    timeout: ep.HttpTimeout ?? options.HttpTimeout,
                    ct: context.RequestAborted)).ToArray();

                var results = await Task.WhenAll(tasks);

                for (var idx = 0; idx < results.Length; idx++)
                {
                    var ep = options.RemoteEndpoints[idx];
                    var res = results[idx];
                    var prefix = string.IsNullOrWhiteSpace(ep.Name) ? ep.Url : ep.Name;

                    if (res.Report?.Entries is not null)
                    {
                        foreach (var kvp in res.Report.Entries)
                        {
                            var key = $"{prefix}|{kvp.Key}";

                            // Avoid collisions by suffixing if needed
                            var finalKey = key;
                            var i = 1;
                            while (mergedEntries.ContainsKey(finalKey))
                            {
                                finalKey = $"{key}#{i++}";
                            }

                            // Ensure entry type is set if missing/unknown
                            var remoteEntry = kvp.Value;

                            remoteEntry.EntryType = InferEntryType(remoteEntry.Tags);
                            mergedEntries[finalKey] = remoteEntry;
                        }
                    }

                    if (options.IncludeRemoteSummaryEntry)
                    {
                        var summaryData = new Dictionary<string, object>
                        {
                            ["StatusCode"] = res.StatusCode,
                        };

                        HealthStatus summaryStatus = HealthStatus.Healthy;
                        string? summaryDescription = null;

                        if (res.Error != null)
                        {
                            summaryStatus = HealthStatus.Unhealthy;
                            summaryDescription = res.Error.Message;
                        }
                        else if (res.Report is not null)
                        {
                            summaryStatus = res.Report.Status;
                        }
                        else if (res.StatusCode != 0)
                        {
                            summaryStatus = res.StatusCode is >= 200 and < 300 ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                        }

                        var summaryKey = prefix;
                        var finalKey = summaryKey;
                        var i2 = 1;
                        while (mergedEntries.ContainsKey(finalKey))
                        {
                            finalKey = $"{summaryKey}#{i2++}";
                        }

                        mergedEntries[finalKey] = new HealthCheckEntry
                        {
                            Status = summaryStatus,
                            Description = summaryDescription,
                            Duration = res.Duration,
                            Data = summaryData,
                            Exception = res.Error?.GetType().FullName,
                            Tags = [],
                            EntryType = "Service"
                        };
                    }
                }
            }

            var response = new HealthCheckReport
            {
                Status = localReport.Status,
                Timestamp = DateTime.UtcNow,
                Duration = localReport.TotalDuration,
                TraceId = context.TraceIdentifier,
                Entries = mergedEntries
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }

        private static async Task<RemoteFetchResult> FetchRemoteAsync(
            IHttpClientFactory? factory,
            string url,
            TimeSpan timeout,
            CancellationToken ct)
        {
            HealthCheckReport? report = null;
            int status = 0;
            TimeSpan duration = TimeSpan.Zero;
            Exception? error = null;

            try
            {
                using var client = factory?.CreateClient(nameof(MonivusHealthCheckExtensions)) ?? new HttpClient();
                client.Timeout = timeout;

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var started = DateTime.UtcNow;
                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                duration = DateTime.UtcNow - started;
                status = (int)resp.StatusCode;

                await using var s = await resp.Content.ReadAsStreamAsync(ct);
                try
                {
                    report = await JsonSerializer.DeserializeAsync<HealthCheckReport>(s, JsonOptions, ct);
                }
                catch (JsonException ex)
                {
                    error = ex;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }

            return new RemoteFetchResult(report, status, duration, error);
        }

        private static string InferEntryType(IEnumerable<string>? tags)
        {
            var firstTag = tags?.FirstOrDefault()?.Trim();

            if (!string.IsNullOrEmpty(firstTag)) return firstTag;

            return "Unknown";
        }

        private sealed record RemoteFetchResult(
            HealthCheckReport? Report,
            int StatusCode,
            TimeSpan Duration,
            Exception? Error);
    }
}
