using System;
using Microphobia.Dashboard.Harness.WebApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using N17Solutions.Microphobia;
using N17Solutions.Microphobia.Dashboard;
using N17Solutions.Microphobia.Extensions;
using N17Solutions.Microphobia.Postgres.Extensions;

namespace Microphobia.Dashboard.Harness.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddScoped<InjectMe>()
                .AddScoped<ComplicatedEnqueueMe>()
                .AddMicrophobiaPostgresStorage(Configuration.GetConnectionString("Microphobia"))
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseMicrophobia();
            app.UseMicrophobiaDashboard();

            app.UseMvc();

            applicationLifetime.ApplicationStopping.Register(() => OnShutdown(serviceProvider.GetRequiredService<Client>()));
        }

        private static void OnShutdown(Client client)
        {
            client.Stop();
        }
    }
}