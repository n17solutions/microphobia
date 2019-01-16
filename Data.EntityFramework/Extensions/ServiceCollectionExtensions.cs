using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using N17Solutions.Microphobia.Data.EntityFramework.Providers;
using N17Solutions.Microphobia.ServiceContract.Providers;

namespace N17Solutions.Microphobia.Data.EntityFramework.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFramework(this IServiceCollection services)
        {
            services.TryAddTransient<IDataProvider, DataProvider>();
            services.TryAddTransient<ISystemLogProvider, SystemLogProvider>();

            return services;
        }
    }
}
