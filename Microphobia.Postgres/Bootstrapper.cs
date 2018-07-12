using System;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Postgres.Extensions;

namespace N17Solutions.Microphobia.Postgres
{
    public static class Bootstrapper
    {
        public static IServiceCollection Strap(string connectionString, Action<IServiceCollection> serviceCollectionAction = null)
        {
            var microphobiaServices = new ServiceCollection();
            microphobiaServices.AddMicrophobiaPostgresStorage(connectionString);

            serviceCollectionAction?.Invoke(microphobiaServices);

            return microphobiaServices;
        }
    }
}
