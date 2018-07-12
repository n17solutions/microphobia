using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.SqlServer.Extensions;
using N17Solutions.Microphobia.Utilities.Configuration;

namespace Microphobia.SqlServer.Harness.Context
{
    class SqlContextCreator : IDesignTimeDbContextFactory<TaskContext>
    {
        public TaskContext CreateDbContext(string[] args)
        {
            var configurationManager = new ConfigurationManager(new ConfigurationBuilder());
            var databaseConfig = configurationManager.Bind<DatabaseConfig>();

            var dbOptions = new DbContextOptionsBuilder<TaskContext>()
                .UseSqlServer(databaseConfig.ConnectionString,
                    x =>
                    {
                        x.MigrationsAssembly("N17Solutions.Microphobia.SqlServer");
                        x.MigrationsHistoryTable(ServiceCollectionExtensions.SqlHistoryTableName, Schema.MicrophobiaSchemaName);
                    })
                .Options;
            
            return new TaskContext(dbOptions);
        }
    }
}
