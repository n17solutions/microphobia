using System;
using System.Reflection;

namespace N17Solutions.Microphobia.Utilities.Configuration
{
    public class ConfigSectionNameAttribute : Attribute
    {
        private const string Config = "Config";
        
        public string Name { get; set; }

        public ConfigSectionNameAttribute(string name)
        {
            Name = name;
        }

        public static string ReadFrom(Type classType)
        {
            var attr = classType.GetCustomAttribute<ConfigSectionNameAttribute>();
            if (attr != null)
                return attr.Name;

            var classTypeName = classType.Name;
            return classTypeName.EndsWith(Config) ? classTypeName.Substring(0, classTypeName.Length - Config.Length) : classTypeName;
        }
    }
}