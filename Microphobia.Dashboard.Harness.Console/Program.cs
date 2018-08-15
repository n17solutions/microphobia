using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Utilities.Configuration;
using Newtonsoft.Json;
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
            
            var task = Task.Run(() =>
            {
                var configuration = new ConfigurationManager(new ConfigurationBuilder());
                var services = PostgresBootstrapper.Strap(configuration.GetConnectionString("Microphobia"));

                serviceProvider = services.BuildServiceProvider();
                client = serviceProvider.GetRequiredService<Client>();

                client?.Start();
                DashboardBootstrapper.Strap(serviceProvider);
            });

            task.ContinueWith(t => System.Console.WriteLine($"An error occurred.{Environment.NewLine}{JsonConvert.SerializeObject(t.Exception)}"), TaskContinuationOptions.OnlyOnFaulted);

            exitEvent.WaitOne();
            System.Console.WriteLine("Exiting");
            client?.Stop();
        }
    }
}
