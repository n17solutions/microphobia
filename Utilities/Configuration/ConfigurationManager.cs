using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace N17Solutions.Microphobia.Utilities.Configuration
{
    public class ConfigurationManager
    {
        private readonly IConfigurationRoot _configuration;

        public ConfigurationManager(IConfigurationBuilder configurationBuilder)
        {
            if (!(configurationBuilder is ConfigurationBuilder builder))
                throw new ArgumentNullException(nameof(builder));

            var basePath = Directory.GetCurrentDirectory();

            builder
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", false, true);
            
            // Add any environment files
            var environmentVars = new ConfigurationBuilder().AddEnvironmentVariables("ASPNETCORE_ENVIRONMENT").Build().GetChildren().Distinct().ToArray();
            if (environmentVars.Any())
            {
                foreach (var envVar in environmentVars)
                {
                    var settingsFile = $"appsettings.{envVar.Value.ToLowerInvariant()}.json";

                    if (!File.Exists(settingsFile))
                        continue;
                    
                    Console.WriteLine($"Adding Settings File: {settingsFile}");
                    builder.AddJsonFile(settingsFile, true);
                }
            }

            _configuration = builder.Build();
        }

        public T Bind<T>(string sectionName = null) where T : class, new()
        {
            var jsonSectionName = (sectionName ?? ConfigSectionNameAttribute.ReadFrom(typeof(T))) ?? typeof(T).Name;
            var bind = new T();
            
            _configuration.GetSection(jsonSectionName).Bind(bind);

            return bind;
        }

        public string GetConnectionString(string name)
        {
            return _configuration.GetConnectionString(name);
        }

        public T GetSetting<T>(string name, T defaultValue = default(T))
        {
            var result = _configuration[name];

            if (string.IsNullOrWhiteSpace(result))
                return defaultValue;

            return (T) Convert.ChangeType(result, typeof(T));
        }
    }
}