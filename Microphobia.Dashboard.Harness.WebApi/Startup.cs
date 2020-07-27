using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microphobia.Dashboard.Harness.WebApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N17Solutions.Microphobia.Dashboard;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.Postgres.Extensions;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Microphobia.Dashboard.Harness.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<InjectMe>()
                .AddTransient<ComplicatedEnqueueMe>()
                .AddMicrophobiaPostgresStorage(Configuration.GetConnectionString("Microphobia"))
                .AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            
            app.UseMicrophobia(config =>
            {
                config.RunnerName = "Web API";
                config.Tag = "WebApi";
            });
            app.UseMicrophobiaDashboard();

            applicationLifetime.ApplicationStopping.Register(async () => await OnShutdown(serviceProvider.GetServices<IHostedService>()));
        }

        private static async Task OnShutdown(IEnumerable<IHostedService> hostedServices)
        {
            if (hostedServices != null)
            {
                var stopTasks = hostedServices.Select(service => service.StopAsync(CancellationToken.None)).ToList();
                await Task.WhenAll(stopTasks);
            }
        }
    }
}