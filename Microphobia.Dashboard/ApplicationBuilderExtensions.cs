using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;

namespace N17Solutions.Microphobia.Dashboard
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMicrophobiaDashboard(this IApplicationBuilder app, Action<MicrophobiaDashboardOptions> setupAction = null)
        {
            var options = new MicrophobiaDashboardOptions();
            setupAction?.Invoke(options);

            var routePrefix = options.RoutePrefix.StartsWith("/") ? options.RoutePrefix : $"/{options.RoutePrefix}";
            
            app.UseSignalR(route => { route.MapHub<MicrophobiaHub>($"{routePrefix}/microphobiahub"); });

            app.UseBranchedApplicationBuilder(routePrefix, services =>
            {
                services.ConfigureDashboardServiceProvider(app.ApplicationServices);
                                                
                services.ConfigureServicesForStandaloneMicrophobiaDashboard();
            }, builderConfig =>
            {
                builderConfig.ConfigureApplicationForMicrophobiaDashboard(options);
            });

            var hubContext = app.ApplicationServices.GetRequiredService<MicrophobiaHubContext>();
            hubContext.ReplaceHubContext(app.ApplicationServices.GetRequiredService<IHubContext<MicrophobiaHub>>());

            return app;
        }

        private static IApplicationBuilder ConfigureApplicationForMicrophobiaDashboard(this IApplicationBuilder app, MicrophobiaDashboardOptions options)
        {
            app.UseMvc();

            app.UseMiddleware<DashboardMiddleware>(options);

            return app;
        }

        private static IApplicationBuilder UseBranchedApplicationBuilder(this IApplicationBuilder app, PathString path, Action<IServiceCollection> servicesConfiguration,
            Action<IApplicationBuilder> appBuilderConfiguration)
        {
            var webHost = new WebHostBuilder()
                .UseKestrel()
                .ConfigureServices(servicesConfiguration)
                .UseStartup<NoopStartup>()
                .Build();

            var serviceProvider = webHost.Services;
            var serverFeatures = webHost.ServerFeatures;
            var appBuilderFactory = serviceProvider.GetRequiredService<IApplicationBuilderFactory>();
            var branchBuilder = appBuilderFactory.CreateBuilder(serverFeatures);
            var factory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            branchBuilder.Use(async (context, next) =>
            {
                using (var scope = factory.CreateScope())
                {
                    context.RequestServices = scope.ServiceProvider;
                    await next();
                }
            });

            appBuilderConfiguration(branchBuilder);

            var branchDelegate = branchBuilder.Build();

            return app.Map(path, builder => { builder.Use(async (context, next) => { await branchDelegate(context); }); });
        }

        private class NoopStartup
        {
            public void ConfigureServices(IServiceCollection services) {}
            public void Configure(IApplicationBuilder app) {}
        }
    }
}