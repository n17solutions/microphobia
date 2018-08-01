﻿using System;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.Postgres.Extensions;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.Postgres
{
    public static class Bootstrapper
    {
        public static IServiceCollection Strap(string connectionString, Action<IServiceCollection> serviceCollectionAction = null, ServiceFactory serviceFactory = null)
        {
            var microphobiaServices = new ServiceCollection();
            microphobiaServices.AddMicrophobiaPostgresStorage(connectionString, serviceFactory);

            serviceCollectionAction?.Invoke(microphobiaServices);

            return microphobiaServices;
        }
    }
}
