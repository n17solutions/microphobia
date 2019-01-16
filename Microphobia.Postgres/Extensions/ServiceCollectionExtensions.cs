using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Data.EntityFramework.Extensions;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.Postgres.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string PostgresTaskContextHistoryTableName = "__MicrophobiaTaskContextMigrationsHistory";
        public const string PostgresSystemLogContextHistoryTableName = "__MicrophobiaSystemLogContextHistory";
        
        private static readonly string CurrentAssemblyName = typeof(ServiceCollectionExtensions).Assembly.GetName().Name;
        
        public static IServiceCollection AddMicrophobiaPostgresStorage(this IServiceCollection services, string connectionString, ServiceFactory serviceFactory = null,
            Action<MicrophobiaConfiguration> configAction = null)
        {
            Migrate(connectionString);

            services
                .AddDbContext<TaskContext>(options => 
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(CurrentAssemblyName);
                    }),ServiceLifetime.Transient, ServiceLifetime.Transient)
                .AddDbContext<SystemLogContext>(options => options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(CurrentAssemblyName);
                }), ServiceLifetime.Transient, ServiceLifetime.Transient)
                .AddEntityFramework()
                .AddMicrophobia();
            
            services.AddSingleton(serviceProvider =>
            {
                var configuration = new MicrophobiaConfiguration(serviceProvider.GetRequiredService<MicrophobiaHubContext>())
                {
                    StorageType = Storage.Postgres,
                    ServiceFactory = serviceFactory ?? serviceProvider.GetService
                };
                
                configAction?.Invoke(configuration);

                return configuration;
            });
            
            return services;
        }

        private static void Migrate(string connectionString)
        {
            void MigrateTaskContext()
            {
                try
                {
                    var migrationOptions = new DbContextOptionsBuilder<TaskContext>().UseNpgsql(
                            connectionString,
                            db =>
                            {
                                db.MigrationsHistoryTable(PostgresTaskContextHistoryTableName, Schema.MicrophobiaSchemaName);
                                db.MigrationsAssembly($"{CurrentAssemblyName}.Migrations.Task");
                            })
                        .Options;

                    var migrationContext = new TaskContext(migrationOptions);
                    var pendingMigrations = migrationContext.Database.GetPendingMigrations();
                    if (pendingMigrations != null && pendingMigrations.Any())
                        migrationContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred migrating the Task Postgres database.", ex);
                }
            }

            void MigrateSystemLogContext()
            {
                try
                {
                    var migrationOptions = new DbContextOptionsBuilder<SystemLogContext>().UseNpgsql(
                            connectionString,
                            db =>
                            {
                                db.MigrationsHistoryTable(PostgresSystemLogContextHistoryTableName, Schema.MicrophobiaSchemaName);
                                db.MigrationsAssembly($"{CurrentAssemblyName}.Migrations.SystemLog");
                            })
                        .Options;

                    var migrationContext = new SystemLogContext(migrationOptions);
                    var pendingMigrations = migrationContext.Database.GetPendingMigrations();
                    if (pendingMigrations != null && pendingMigrations.Any())
                        migrationContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    throw new Exception("An error occurred migrating the System Log Postgres database.", ex);
                }   
            }
            
            MigrateTaskContext();
            MigrateSystemLogContext();
        }
    }
}
