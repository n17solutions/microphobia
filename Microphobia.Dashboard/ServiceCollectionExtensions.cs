using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;

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
            services.AddTransient(_ =>
            {
                var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
                var scope = serviceScopeFactory.CreateScope();
                return scope.ServiceProvider.GetRequiredService<IDataProvider>();
            });
            
            
            var hostedServices = serviceProvider.GetServices<IHostedService>() ?? Enumerable.Empty<IHostedService>();
            var client = hostedServices.FirstOrDefault(service => service.GetType() == typeof(Client));
            if (client == null)
                throw new InvalidOperationException("Microphobia Client hasn't been registered. Be sure to call the data provider add services method before initialising the dashboard.");

            services.AddSingleton(_ => client);
        }
    }
}