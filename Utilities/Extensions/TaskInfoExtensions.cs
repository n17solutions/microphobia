using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class TaskInfoExtensions
    {
        public static object Execute(this TaskInfo taskInfo, ServiceFactory serviceFactory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var assembly = Assembly.Load(taskInfo.AssemblyName);
            var type = assembly.GetType(taskInfo.TypeName);

            var methodName = taskInfo.MethodName;
            var args = taskInfo.Arguments;

            object instance = null;
            MethodInfo method;
            method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                instance = serviceFactory != null ? serviceFactory(type) : Activator.CreateInstance(type);
                if (instance == null)
                    throw new InvalidOperationException($"Cannot execute method {methodName} on type {type} as it cannot be resolved. Have you made sure to add it to your Dependency Injection?");

                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            var result = InvokeMethod(method, instance, args, cancellationToken);
            return result;
        }

        private static object InvokeMethod(MethodInfo method, object instance, object[] arguments, CancellationToken cancellationToken)
        {
            var result = method.Invoke(instance, arguments);

            if (!(result is Task task)) 
                return result;
            
            task.Wait(cancellationToken);

            if (method.ReturnType.GetTypeInfo().IsGenericType)
            {
                var resultProperty = method.ReturnType.GetRuntimeProperty("Result");
                result = resultProperty.GetValue(task);
            }
            else
            {
                result = null;
            }

            return result;
        }
    }
}