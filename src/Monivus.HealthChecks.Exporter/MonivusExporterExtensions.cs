using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monivus.HealthChecks.Exporter;

namespace Monivus.HealthChecks
{
    /// <summary>
    /// Provides extension methods for configuring and registering Monivus exporter services in an application.
    /// </summary>
    /// <remarks>This class includes methods to simplify the setup of Monivus exporter services, such as
    /// binding configuration options and registering required runtime services. It is intended to be used during
    /// application startup to integrate Monivus exporter functionality into the dependency injection
    /// container.</remarks>
    public static class MonivusExporterExtensions
    {
        /// <summary>
        /// Adds and configures the Monivus exporter services to the provided <see cref="IServiceCollection"/>.
        /// Binds <see cref="MonivusExporterOptions"/> to the "Monivus:Exporter" configuration section, and registers
        /// the required runtime services such as an <see cref="System.Net.Http.HttpClient"/> and the
        /// <see cref="MonivusExporterBackgroundService"/> hosted service.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        /// <param name="configuration">The application configuration used to bind exporter options.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance to allow chaining of additional registrations.</returns>
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
