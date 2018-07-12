using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Utilities.Configuration;
using PostgresBootstrapper = N17Solutions.Microphobia.Postgres.Bootstrapper;
using DashboardBootstrapper = N17Solutions.Microphobia.Dashboard.Bootstrapper;

namespace N17Solutions.Microphobia.Dashboard.Harness.Console
{
    class Program
    {
        private static readonly object Lock = new object();
        
        static void Main(string[] args)
        {
            Client client = null;
            IServiceProvider serviceProvider = null;
            
            var exitEvent = new ManualResetEvent(false);
            
            System.Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { exitEvent.Set(); };
            
            Task.Run(() =>
            {
                var configuration = new ConfigurationManager(new ConfigurationBuilder());
                var services = PostgresBootstrapper.Strap(configuration.GetConnectionString("Microphobia"));

                lock (Lock)
                {
                    serviceProvider = services.BuildServiceProvider();
                    client = serviceProvider.GetRequiredService<Client>();
                }

                client?.Start();

                DashboardBootstrapper.Strap(services);
            });

            exitEvent.WaitOne();
            System.Console.WriteLine("Exiting");
            client?.Stop();
        }
    }
}
