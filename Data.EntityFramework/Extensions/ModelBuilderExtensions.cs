using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace N17Solutions.Microphobia.Data.EntityFramework.Extensions
{
    public static class ModelBuilderExtensions
    {
        private static readonly MethodInfo EntityMethod = typeof(ModelBuilder).GetTypeInfo().GetMethods().Single(x => x.Name.Equals("Entity") && x.IsGenericMethod && x.GetParameters().Length == 0);
        private static readonly Dictionary<Assembly, IEnumerable<Type>> TypesPerAssembly = new Dictionary<Assembly, IEnumerable<Type>>();
        
        public static ModelBuilder UseEntityTypeConfiguration(this ModelBuilder modelBuilder, Assembly assembly)
        {
            if (!TypesPerAssembly.TryGetValue(assembly, out var configurationTypes))
                TypesPerAssembly[assembly] = configurationTypes = assembly.GetExportedTypes()
                    .Where(x => x.GetTypeInfo().IsClass && !x.GetTypeInfo().IsAbstract && x.GetInterfaces().Any(y => y.GetTypeInfo().IsGenericType && y.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

            var configurations = configurationTypes.Select(x => Activator.CreateInstance(x.GetTypeInfo()));

            foreach (dynamic configuration in configurations)
                ApplyConfiguration(modelBuilder, configuration);

            return modelBuilder;
        }

        public static ModelBuilder ApplyConfiguration<T>(this ModelBuilder modelBuilder, IEntityTypeConfiguration<T> configuration) where T : class
        {
            Type FindEntityType(Type type)
            {
                var interfaceType = type.GetInterfaces().First(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));
                return interfaceType.GetGenericArguments().First();
            }

            var entityType = FindEntityType(configuration.GetType());
            
            if (modelBuilder.Model.FindEntityType(entityType) == null)
                return modelBuilder;
            
            dynamic entityTypeBuilder = EntityMethod.MakeGenericMethod(entityType).Invoke(modelBuilder, new object[0]);
            
            configuration.Configure(entityTypeBuilder);

            return modelBuilder;
        }
    }
}