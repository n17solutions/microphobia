using System;
using System.Reflection;
using N17Solutions.Microphobia.ServiceContract.Exceptions;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class TaskInfoExtensions
    {
        public static object Execute(this TaskInfo taskInfo, ServiceFactory serviceFactory = null)
        {
            var assembly = Assembly.Load(taskInfo.AssemblyName);
            var type = assembly.GetType(taskInfo.TypeName);

            var methodName = taskInfo.MethodName;
            var args = taskInfo.Arguments;
            
            var staticMethod = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (staticMethod != null)
                return staticMethod.Invoke(null, args);

            var instanceMethod = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (instanceMethod == null)
                throw new UnableToDeserializeDelegateException();

            var instance = serviceFactory != null
                ? serviceFactory(type)
                : Activator.CreateInstance(type);
            
            if (instance == null)
                throw new InvalidOperationException($"Cannot execute method {methodName} on type {type} as it cannot be resolved. Have you made sure to add it to your Dependency Injection?");
            
            return instanceMethod.Invoke(instance, args);
        }
    }
}