using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Monivus.HealthChecks.Exporter
{
    public static class MonivusExporterServiceCollectionExtensions
    {
        public static IServiceCollection AddMonivusExporter(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = MonivusExporterOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<MonivusExporterOptions>()
                .Bind(configuration.GetSection(sectionName))
                .PostConfigure(options => options.Normalize());

            return RegisterCoreServices(services);
        }

        public static IServiceCollection AddMonivusExporter(
            this IServiceCollection services,
            Action<MonivusExporterOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            services.AddOptions<MonivusExporterOptions>()
                .Configure(configure)
                .PostConfigure(options => options.Normalize());

            return RegisterCoreServices(services);
        }

        private static IServiceCollection RegisterCoreServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.TryAddSingleton<MonivusExporterRegistrationMarker>();
            services.AddHostedService<MonivusExporterBackgroundService>();
            return services;
        }
    }
}
