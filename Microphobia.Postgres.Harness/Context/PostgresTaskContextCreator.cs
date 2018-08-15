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
    class PostgresTaskContextCreator : IDesignTimeDbContextFactory<TaskContext>
    {
        public TaskContext CreateDbContext(string[] args)
        {
            var configurationManager = new ConfigurationManager(new ConfigurationBuilder());
            var databaseConfig = configurationManager.Bind<DatabaseConfig>();

            var dbOptions = new DbContextOptionsBuilder<TaskContext>()
                .UseNpgsql(databaseConfig.ConnectionString,
                    x =>
                    {
                        x.MigrationsAssembly("N17Solutions.Microphobia.Postgres.Migrations.Task");
                        x.MigrationsHistoryTable(ServiceCollectionExtensions.PostgresTaskContextHistoryTableName, Schema.MicrophobiaSchemaName);
                    })
                .Options;
            
            return new TaskContext(dbOptions);
        }
    }
}