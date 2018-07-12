using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.Utilities.Configuration;

namespace Microphobia.Dashboard.Harness.WebApi.Data.Context
{
    public class PostgresHarnessContextCreator : IDesignTimeDbContextFactory<PostgresHarnessContext>
    {
        public PostgresHarnessContext CreateDbContext(string[] args)
        {
            var configurationManager = new ConfigurationManager(new ConfigurationBuilder());
            var databaseConfig = configurationManager.Bind<DatabaseConfig>("Postgres");

            var dbOptions = new DbContextOptionsBuilder<PostgresHarnessContext>()
                .UseNpgsql(databaseConfig.ConnectionString)
                .Options;
            return new PostgresHarnessContext(dbOptions);
        }
    }
}