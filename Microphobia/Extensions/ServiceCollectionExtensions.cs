using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;

namespace N17Solutions.Microphobia.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMicrophobia(this IServiceCollection services)
        {
            var markerClassRegistered = services.BuildServiceProvider().GetService<MicrophobiaMarkerClass>();
            if (markerClassRegistered != null)
                return services;
            
            services.AddSignalR();

            return services
                .AddSingleton<MicrophobiaMarkerClass>()
                .AddSingleton(serviceProvider => new MicrophobiaHubContext(serviceProvider.GetRequiredService<IHubContext<MicrophobiaHub>>()))
                .AddScoped<Queue>()
                .AddScoped<Runners>()
                .AddSingleton<IHostedService, Client>();
        }
    }
}