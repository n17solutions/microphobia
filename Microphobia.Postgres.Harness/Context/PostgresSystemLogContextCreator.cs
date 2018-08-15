using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Postgres.Extensions;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.Utilities.Configuration;

namespace N17Solutions.Microphobia.Postgres.Harness.Context
{
    class PostgresSystemLogContextCreator : IDesignTimeDbContextFactory<SystemLogContext>
    {
        public SystemLogContext CreateDbContext(string[] args)
        {
            var configurationManager = new ConfigurationManager(new ConfigurationBuilder());
            var databaseConfig = configurationManager.Bind<DatabaseConfig>();

            var dbOptions = new DbContextOptionsBuilder<SystemLogContext>()
                .UseNpgsql(databaseConfig.ConnectionString,
                    x =>
                    {
                        x.MigrationsAssembly("N17Solutions.Microphobia.Postgres.Migrations.SystemLog");
                        x.MigrationsHistoryTable(ServiceCollectionExtensions.PostgresSystemLogContextHistoryTableName, Schema.MicrophobiaSchemaName);
                    })
                .Options;
            
            return new SystemLogContext(dbOptions);
        }
    }
}