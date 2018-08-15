using System;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Data.EntityFramework.Providers;
using N17Solutions.Microphobia.ServiceContract.Providers;

namespace N17Solutions.Microphobia.Data.EntityFramework.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFramework(this IServiceCollection services)
        {
            services.AddTransient<IDataProvider, DataProvider>();
            services.AddTransient<ISystemLogProvider, SystemLogProvider>();
            return services;
        }
    }
}
