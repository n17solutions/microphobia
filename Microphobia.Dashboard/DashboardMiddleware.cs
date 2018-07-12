using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract.Configuration;

namespace N17Solutions.Microphobia.Dashboard
{
    public class DashboardMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MicrophobiaDashboardOptions _options;
        private readonly MicrophobiaConfiguration _config;

        public DashboardMiddleware(RequestDelegate next, MicrophobiaConfiguration config, MicrophobiaDashboardOptions options)
        {
            _next = next;
            _config = config;
            _options = options;
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
                    
                    var originalStream = httpContext.Response.Body;
                    
                    using (var newStream = new MemoryStream())
                    {
                        httpContext.Response.Body = newStream;
                        await _next(httpContext);
                        
                        newStream.Seek(0, SeekOrigin.Begin);
                        
                        var html = await new StreamReader(newStream).ReadToEndAsync();
                        var htmlBuilder = new StringBuilder(html);
                        foreach (var entry in GetIndexParameters())
                        {
                            htmlBuilder.Replace(entry.Key, entry.Value);
                        }

                        newStream.Seek(0, SeekOrigin.Begin);

                        httpContext.Response.Body = originalStream;
                        httpContext.Response.ContentLength = htmlBuilder.ToString().Length;
                        await httpContext.Response.WriteAsync(htmlBuilder.ToString());
                    }

                    return;
            }

            await _next(httpContext);
        }

        private IDictionary<string, string> GetIndexParameters()
        {
            return new Dictionary<string, string>
            {
                {"%(DocumentTitle)", _options.DocumentTitle},
                {"%(StorageInUse)", _config.StorageType.ToString()}
            };
        }
    }
}