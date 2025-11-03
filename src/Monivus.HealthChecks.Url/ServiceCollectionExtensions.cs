using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monivus.HealthChecks.Url
{
    public static class ServiceCollectionExtensions
    {
        // Optional helper to bind defaults from configuration
        // Example: services.ConfigureUrlHealthChecks(builder.Configuration.GetSection("UrlHealthCheck"));
        public static IServiceCollection ConfigureUrlHealthChecks(
            this IServiceCollection services,
            IConfiguration configurationSection)
        {
            services.Configure<UrlHealthCheckOptions>(configurationSection);
            return services;
        }
    }
}

