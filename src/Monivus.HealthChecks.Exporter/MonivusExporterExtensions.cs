using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monivus.HealthChecks.Exporter;

namespace Monivus.HealthChecks
{
    public static class MonivusExporterExtensions
    {
        public static IServiceCollection AddMonivusExporter(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<MonivusExporterOptions>()
                .Bind(configuration.GetSection("Monivus:Exporter"));

            return RegisterCoreServices(services);
        }

        private static IServiceCollection RegisterCoreServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHostedService<MonivusExporterBackgroundService>();
            return services;
        }
    }
}
