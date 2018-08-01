using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Websockets.Hubs;

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

        public static void ConfigureDashboardServiceProvider(this IServiceCollection services, IServiceProvider serviceProvider)
        {
            services.AddSignalR();
            
            services.AddSingleton(_ => serviceProvider.GetRequiredService<MicrophobiaConfiguration>());
            services.AddSingleton(_ => serviceProvider.GetRequiredService<MicrophobiaHubContext>());
            services.AddTransient(_ => serviceProvider.GetRequiredService<IDataProvider>());            
        }
    }
}