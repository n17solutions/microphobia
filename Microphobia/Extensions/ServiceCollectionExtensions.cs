using Microsoft.Extensions.DependencyInjection;

namespace N17Solutions.Microphobia.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMicrophobia(this IServiceCollection services)
        {
            services.AddSignalR();
            
            return services
                .AddScoped<Queue>()
                .AddScoped<Client>();
        }
    }
}