using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Monivus.HealthChecks
{
    public sealed class ResourceUtilizationHealthCheck : IHealthCheck
    {
        private readonly ResourceUtilizationHealthCheckOptions _options;

        public ResourceUtilizationHealthCheck(ResourceUtilizationHealthCheckOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var process = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            if (uptime < TimeSpan.Zero)
            {
                uptime = TimeSpan.Zero;
            }

            var gcInfo = GC.GetGCMemoryInfo();
            var totalAllocatedBytes = GC.GetTotalAllocatedBytes();
            var memoryUsagePercent = CalculateMemoryUsagePercent(gcInfo);

            var metrics = new Dictionary<string, object>
            {
                ["ProcessName"] = process.ProcessName,
                ["Is64BitProcess"] = Environment.Is64BitProcess,
                ["ProcessorCount"] = Environment.ProcessorCount,
                ["UptimeSeconds"] = Math.Round(uptime.TotalSeconds, 2),
                ["TotalProcessorTimeSeconds"] = Math.Round(process.TotalProcessorTime.TotalSeconds, 2),
                ["MemoryUsagePercent"] = memoryUsagePercent,
                ["WorkingSetBytes"] = process.WorkingSet64,
                ["WorkingSetMegabytes"] = Math.Round(process.WorkingSet64 / 1024d / 1024d, 2),
                ["PrivateMemoryBytes"] = process.PrivateMemorySize64,
                ["PagedMemoryBytes"] = process.PagedMemorySize64,
                ["PagedSystemMemoryBytes"] = process.PagedSystemMemorySize64,
                ["NonPagedSystemMemoryBytes"] = process.NonpagedSystemMemorySize64,
                ["EnvironmentWorkingSetBytes"] = Environment.WorkingSet,
                ["HandleCount"] = process.HandleCount,
                ["ThreadCount"] = process.Threads.Count,
                ["PriorityClass"] = process.PriorityClass.ToString(),
                ["GcTotalAllocatedBytes"] = totalAllocatedBytes,
                ["GcHeapSizeBytes"] = gcInfo.HeapSizeBytes,
                ["GcFragmentedBytes"] = gcInfo.FragmentedBytes,
                ["GcCommittedBytes"] = gcInfo.TotalCommittedBytes,
                ["GcMemoryLoadBytes"] = gcInfo.MemoryLoadBytes,
                ["GcHighMemoryLoadThresholdBytes"] = gcInfo.HighMemoryLoadThresholdBytes,
                ["GcTotalAvailableMemoryBytes"] = gcInfo.TotalAvailableMemoryBytes,
                ["GcCollectionsGen0"] = GC.CollectionCount(0),
                ["GcCollectionsGen1"] = GC.CollectionCount(1),
                ["GcCollectionsGen2"] = GC.CollectionCount(2),
                ["GcPinnedObjectsCount"] = gcInfo.PinnedObjectsCount,
            };

            if (gcInfo.Generation >= 0)
            {
                metrics["GcGeneration"] = gcInfo.Generation;
            }

            if (gcInfo.PauseTimePercentage >= 0)
            {
                metrics["GcPauseTimePercentage"] = Math.Round(gcInfo.PauseTimePercentage, 2);
            }

            var status = HealthStatus.Healthy;
            var description = "Resource utilization within defined thresholds.";

            if (_options.MemoryUsageDegradedThresholdPercent != 0 && memoryUsagePercent >= _options.MemoryUsageDegradedThresholdPercent)
            {
                status = HealthStatus.Degraded;
                description = $"Process memory usage {Math.Round(memoryUsagePercent, 2)}% exceeds {_options.MemoryUsageDegradedThresholdPercent}% threshold.";
            }

            return Task.FromResult(new HealthCheckResult(
                status,
                description,
                null,
                metrics));
        }

        private static double CalculateMemoryUsagePercent(GCMemoryInfo gcInfo)
        {
            var memoryUsage =  gcInfo.TotalAvailableMemoryBytes > 0
                ? gcInfo.MemoryLoadBytes / (double)gcInfo.TotalAvailableMemoryBytes * 100 : 0;

            return Math.Round(Math.Clamp(memoryUsage, 0, 100), 2);
        }
    }
}
