using System;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Models;

namespace N17Solutions.Microphobia.ServiceContract.Providers
{
    public interface ISystemLogProvider
    {
        /// <summary>
        /// Logs a System Log entry to the System Log
        /// </summary>
        /// <param name="log">The <see cref="SystemLog" /> object to log</param>
        /// <param name="storageType">The storage mechanism currently in use - helps generate the correct resource id</param>
        /// <returns>The Resource Id of this Log Entry</returns>
        Task<Guid> Log(SystemLog log, Storage storageType);
    }
}