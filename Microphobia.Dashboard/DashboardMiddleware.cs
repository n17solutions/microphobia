using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using N17Solutions.Microphobia.ServiceContract.Configuration;

namespace N17Solutions.Microphobia.Dashboard
{
    public class DashboardMiddleware
    {
        private const string EmbeddedFileNamespace = "N17Solutions.Microphobia.Dashboard.wwwroot.build";
        
        private readonly MicrophobiaDashboardOptions _options;
        private readonly MicrophobiaConfiguration _config;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        
        public DashboardMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory, MicrophobiaConfiguration config, 
            IOptions<MicrophobiaDashboardOptions> optionsAccessor)
            :this(next, hostingEnv, loggerFactory, config, optionsAccessor.Value)
        {}

        public DashboardMiddleware(RequestDelegate next, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory, MicrophobiaConfiguration config,
            MicrophobiaDashboardOptions options)
        {
            _config = config;
            _options = options ?? new MicrophobiaDashboardOptions();
            _staticFileMiddleware = CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpMethod = httpContext.Request.Method;
            var path = string.IsNullOrEmpty(httpContext.Request.Path)
                ? httpContext.Request.PathBase.Value
                : httpContext.Request.Path.Value;

            switch (httpMethod)
            {
                // If the RoutePrefix is requested (with or without trailing slash), redirect to index URL
                case "GET" when Regex.IsMatch(path, $"^/{_options.RoutePrefix}/?$"):
                    // Use relative redirect to support proxy environments
                    var relativeRedirectPath = path.EndsWith("/")
                        ? "index.html"
                        : $"{path.Split('/').Last()}/index.html";

                    httpContext.Response.StatusCode = 301;
                    httpContext.Response.Headers["Location"] = relativeRedirectPath;
                    return;
                
                case "GET" when Regex.IsMatch(path, $"/{_options.RoutePrefix}/?index.html"):
                case "GET" when Regex.IsMatch(path, "/?index.html"):
                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/html";

                    using (var stream = _options.IndexStream())
                    {
                        var html = await new StreamReader(stream).ReadToEndAsync();
                        var htmlBuilder = new StringBuilder(html);

                        foreach (var entry in GetIndexParameters())
                            htmlBuilder.Replace(entry.Key, entry.Value);

                        await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);
                    }

                    return;
            }

            await _staticFileMiddleware.Invoke(httpContext);
        }

        private IDictionary<string, string> GetIndexParameters()
        {
            return new Dictionary<string, string>
            {
                {"%(DocumentTitle)", _options.DocumentTitle},
                {"%(StorageInUse)", _config.StorageType.ToString()},
                {"%(AssemblyVersion)", PlatformServices.Default.Application.ApplicationVersion}
            };
        }
        
        private StaticFileMiddleware CreateStaticFileMiddleware(
            RequestDelegate next,
            IHostingEnvironment hostingEnv,
            ILoggerFactory loggerFactory,
            MicrophobiaDashboardOptions options)
        {
            var staticFileOptions = new StaticFileOptions
            {               
                FileProvider = new EmbeddedFileProvider(typeof(DashboardMiddleware).GetTypeInfo().Assembly, EmbeddedFileNamespace),
            };

            return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }
    }
}