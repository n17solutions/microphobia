using System;
using System.Collections.Generic;

namespace N17Solutions.Microphobia.ServiceResolution
{
    /// <summary>
    /// Factory method used to resolve services. For multiple instances, it will resolve against <see cref="IEnumerable{T}" />
    /// </summary>
    /// <param name="serviceType">Type of service to resolve.</param>
    /// <returns>An instance of type <paramref name="serviceType" /></returns>
    public delegate object ServiceFactory(Type serviceType);
}