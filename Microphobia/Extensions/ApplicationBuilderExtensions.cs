using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.ServiceContract.Configuration;

namespace N17Solutions.Microphobia.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMicrophobia(this IApplicationBuilder app, Action<MicrophobiaConfiguration> configAction = null)
        {
            var config = app.ApplicationServices.GetService<MicrophobiaConfiguration>();
            configAction?.Invoke(config);
            
            var client = app.ApplicationServices.GetRequiredService<Client>();
            client.Start();
            
            return app;
        }
    }
}