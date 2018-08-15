using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using N17Solutions.Microphobia.ServiceContract.Models;

namespace N17Solutions.Microphobia.Utilities.Extensions
{
    public static class MethodInfoExtensions
    {
        public static TaskInfo ToTaskInfo(this MethodInfo method, object[] arguments)
        {
            var taskInfo = new TaskInfo
            {
                Id = Guid.NewGuid(),
                AssemblyName = method.DeclaringType?.Assembly.FullName,
                TypeName = method.DeclaringType?.FullName,
                MethodName = method.Name,
                Arguments = arguments,
                ReturnType = method.ReturnType
            };

            return taskInfo;
        }
    }
}