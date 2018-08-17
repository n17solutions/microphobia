using Microsoft.EntityFrameworkCore;
using N17Solutions.Microphobia.Domain.Logs;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class SystemLogContext : MicrophobiaContext
    {
        #region Aggregate Roots
        public DbSet<SystemLog> Logs { get; set; }
        #endregion

        public SystemLogContext(DbContextOptions<SystemLogContext> options) : base(options, typeof(SystemLogContext).Assembly) { }
    }
}