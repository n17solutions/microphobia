using Microsoft.EntityFrameworkCore;
using N17Solutions.Microphobia.Domain.Clients;
using N17Solutions.Microphobia.Domain.Tasks;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class TaskContext : MicrophobiaContext
    {
        #region Aggregate Roots
        public DbSet<TaskInfo> Tasks { get; set; }
        public DbSet<QueueRunner> Runners { get; set; }
        #endregion

        public TaskContext(DbContextOptions<TaskContext> options) : base(options, typeof(TaskContext).Assembly) { }
    }
}