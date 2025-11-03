using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Monivus.HealthChecks.Exporter
{
    public static class MonivusExporterApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMonivusExporter(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("MonivusExporter");

            if (app.ApplicationServices.GetService<MonivusExporterRegistrationMarker>() == null)
            {
                logger?.LogWarning("UseMonivusExporter was called but AddMonivusExporter was not used. The exporter background service is not registered.");
            }
            else
            {
                logger?.LogInformation("Monivus exporter configured.");
            }

            return app;
        }
    }
}
