using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N17Solutions.Microphobia.Postgres.Extensions;
using N17Solutions.Microphobia.Utilities.Configuration;
using PostgresBootstrapper = N17Solutions.Microphobia.Postgres.Bootstrapper;
using DashboardBootstrapper = N17Solutions.Microphobia.Dashboard.Bootstrapper;

namespace N17Solutions.Microphobia.Dashboard.Harness.Console
{
    static class Enqueuer
    {
        public static void EnqueueMe() => System.Console.WriteLine("HELLO!");
    }
    
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var configuration = new ConfigurationManager(new ConfigurationBuilder());
            var connectionString = configuration.GetConnectionString("Microphobia");
            
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMicrophobiaPostgresStorage(connectionString, configAction: config =>
                    {
                        config.Tag = "Console Tag";
                        config.RunnerName = "Console Runner";
                    });

                    var serviceProvider = services.BuildServiceProvider();
                    DashboardBootstrapper.Strap(serviceProvider);
                });
            
            await builder.RunConsoleAsync();
        }
    }
}
