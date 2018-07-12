using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Extensions;

namespace N17Solutions.Microphobia.Dashboard
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureServicesForStandaloneMicrophobiaDashboard(this IServiceCollection services)
        {
            services
                .AddMvcCore()
                .AddJsonFormatters()
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.FeatureProviders.Clear();
                    manager.FeatureProviders.Add(new MicrophobiaControllerFeatureProvider<DashboardController>());
                });

            return services;
        }
    }
}