using System.Collections.Generic;

namespace N17Solutions.Microphobia.ServiceResolution
{
    public static class ServiceFactoryExtensions
    {
        public static T GetInstance<T>(this ServiceFactory serviceFactory) => (T) serviceFactory(typeof(T));
        public static IEnumerable<T> GetInstances<T>(this ServiceFactory serviceFactory) => (IEnumerable<T>) serviceFactory(typeof(IEnumerable<T>));
    }
}