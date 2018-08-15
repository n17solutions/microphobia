using System;
using System.Threading.Tasks;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;

namespace N17Solutions.Microphobia.Data.EntityFramework.Providers
{
    public class SystemLogProvider : ISystemLogProvider
    {
        private readonly SystemLogContext _context;

        public SystemLogProvider(SystemLogContext context)
        {
            _context = context;
        }

        public async Task<Guid> Log(SystemLog log, Storage storageType)
        {
            var domainObject = Domain.Logs.SystemLog.FromSystemLogResponse(log, storageType);
            await _context.Logs.AddAsync(domainObject).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            return domainObject.ResourceId;
        }
    }
}