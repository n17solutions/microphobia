using System;
using System.Linq;
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

namespace N17Solutions.Microphobia.Postgres.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string PostgresHistoryTableName = "__MicrophobiaMigrationsHistory";
        
        public static IServiceCollection AddMicrophobiaPostgresStorage(this IServiceCollection services, string connectionString)
        {
            Migrate(connectionString);

            services
                .AddDbContext<TaskContext>(options => options.UseNpgsql(connectionString), ServiceLifetime.Transient)
                .AddEntityFramework()
                .AddMicrophobia();
            
            services.AddSingleton(serviceProvider => new MicrophobiaConfiguration(serviceProvider.GetRequiredService<MicrophobiaHubContext>())
            {
                StorageType = Storage.Postgres,
                ServiceFactory = serviceProvider.GetService
            });
            
            return services;
        }

        private static void Migrate(string connectionString)
        {
            try
            {
                var migrationOptions = new DbContextOptionsBuilder<TaskContext>().UseNpgsql(
                        connectionString,
                        db =>
                        {
                            db.MigrationsHistoryTable(PostgresHistoryTableName, Schema.MicrophobiaSchemaName);
                            db.MigrationsAssembly(typeof(ServiceCollectionExtensions).Assembly.FullName);
                        })
                    .Options;

                var migrationContext = new TaskContext(migrationOptions);
                var pendingMigrations = migrationContext.Database.GetPendingMigrations();
                if (pendingMigrations != null && pendingMigrations.Any())
                    migrationContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred migrating the Postgres database.", ex);
            }
        }
    }
}
