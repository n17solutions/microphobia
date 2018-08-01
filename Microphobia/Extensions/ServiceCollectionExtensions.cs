using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace N17Solutions.Microphobia.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMicrophobia(this IServiceCollection services)
        {
            services.AddSignalR();
            
            return services
                .AddSingleton(serviceProvider => new MicrophobiaHubContext(serviceProvider.GetRequiredService<IHubContext<MicrophobiaHub>>()))
                .AddScoped<Queue>()
                .AddScoped<Client>();
        }
    }
}