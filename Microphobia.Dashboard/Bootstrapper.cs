using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.ServiceContract.Providers;

namespace N17Solutions.Microphobia.Dashboard
{
    public static class Bootstrapper
    {
        public static void Strap(IServiceProvider serviceProvider, Action<MicrophobiaDashboardOptions> setupAction = null)
        {
            var options = new MicrophobiaDashboardOptions();
            setupAction?.Invoke(options);

            WebHost.CreateDefaultBuilder(null)
                .ConfigureServices(servicesCollection =>
                {
                    servicesCollection.ConfigureDashboardServiceProvider(serviceProvider);
                })
                .UseStartup(typeof(DashboardStartup))
                .UseUrls($"{options.DashboardUri}")
                .Build()
                .Start();
        }
    }
    
    public class DashboardStartup
    {   
        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureServicesForStandaloneMicrophobiaDashboard();            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseMicrophobiaDashboard();
        }
    }
}