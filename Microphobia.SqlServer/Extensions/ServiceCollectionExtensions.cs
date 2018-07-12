using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Data.EntityFramework.Extensions;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace N17Solutions.Microphobia.SqlServer.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string SqlHistoryTableName = "__MicrophobiaMigrationsHistory";
        
        public static IServiceCollection AddMicrophobiaSqlServerStorage(this IServiceCollection services, string connectionString)
        {
            services
                .AddDbContext<TaskContext>(options => options.UseSqlServer(
                    connectionString,
                    db =>
                    {
                        db.MigrationsHistoryTable(SqlHistoryTableName, Schema.MicrophobiaSchemaName);
                        db.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName);
                    }))
                .AddEntityFramework()
                .AddMicrophobia();

            services.AddSingleton(serviceProvider => new MicrophobiaConfiguration(serviceProvider.GetRequiredService<IHubContext<MicrophobiaHub>>())
            {
                StorageType = Storage.SqlServer,
                ServiceFactory = serviceProvider.GetService
            });

            return services;
        }
    }
}
