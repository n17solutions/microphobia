using System;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace N17Solutions.Microphobia.Dashboard
{
    public static class Bootstrapper
    {
        public static void Strap(IServiceCollection services, Action<MicrophobiaDashboardOptions> setupAction = null)
        {
            var options = new MicrophobiaDashboardOptions();
            setupAction?.Invoke(options);
            
            WebHost.CreateDefaultBuilder(null)
                .UseStartup(typeof(DashboardStartup), services)
                .UseUrls($"{options.DashboardUri}")
                .Build()
                .Run();
        }
    }
    
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseStartup(this IWebHostBuilder hostBuilder, Type startupType, IServiceCollection serviceCollection)
        {
            var name = startupType.GetTypeInfo().Assembly.GetName().Name;
            return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, name).ConfigureServices(services =>
            {
                foreach (var service in serviceCollection)
                    services.TryAdd(service);

                if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                {
                    services.AddSingleton(typeof(IStartup), startupType);
                }
                else
                {
                    services.AddSingleton(typeof(IStartup), (Func<IServiceProvider, object>) (sp =>
                    {
                        var requiredService = sp.GetRequiredService<IHostingEnvironment>();
                        return (object) new ConventionBasedStartup(StartupLoader.LoadMethods(sp, startupType, requiredService.EnvironmentName));
                    }));
                }
            });
        }
    }

    public class DashboardStartup
    {
        private IServiceCollection _serviceCollection;
        
        public DashboardStartup() { }
        
        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureServicesForStandaloneMicrophobiaDashboard();
            _serviceCollection = services;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseMicrophobiaDashboard(options => { options.IsDevelopment = true; }, _serviceCollection);
        }
    }
}