using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.Utilities.Configuration;
using N17Solutions.Microphobia.Websockets.Hubs;
using PostgresBootstrapper = N17Solutions.Microphobia.Postgres.Bootstrapper;
using DashboardBootstrapper = N17Solutions.Microphobia.Dashboard.Bootstrapper;

namespace N17Solutions.Microphobia.Dashboard.Harness.Console
{
    static class Enqueuer
    {
        public static void EnqueueMe() => System.Console.WriteLine("HELLO!");
    }
    
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
                DashboardBootstrapper.Strap(serviceProvider);
            });

            exitEvent.WaitOne();
            System.Console.WriteLine("Exiting");
            client?.Stop();
        }
    }
}
