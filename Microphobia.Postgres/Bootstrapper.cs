using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.Postgres.Extensions;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.Postgres
{
    public static class Bootstrapper
    {
        public static IServiceCollection Strap(string connectionString, ServiceFactory serviceFactory = null)
        {
            var microphobiaServices = new ServiceCollection();
            microphobiaServices.AddMicrophobiaPostgresStorage(connectionString, serviceFactory);

            AppDomain.CurrentDomain.UnhandledException += async (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                var serviceProvider = microphobiaServices.BuildServiceProvider();
                var systemLogger = serviceProvider.GetRequiredService<ISystemLogProvider>();

                Console.WriteLine("LOGGING ERROR");
                await systemLogger.Log(new SystemLog
                {
                    Message = exception?.Message,
                    StackTrace = exception?.StackTrace,
                    Source = exception?.Source,
                    Level = LogLevel.Error,
                    Data = args.ExceptionObject
                }, Storage.Postgres);
            };

            return microphobiaServices;
        }
    }
}
