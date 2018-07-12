using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Data.EntityFramework.Extensions;
using N17Solutions.Microphobia.Domain.Tasks;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class TaskContext : DbContext
    {
        private static readonly object Lock = new object();
        private readonly Assembly _efAssembly = typeof(TaskContext).Assembly;
        private ContextBehavior _contextBehavior = ContextBehavior.Readonly;
        
        #region Aggregate Roots
        public DbSet<TaskInfo> Tasks { get; set; }
        #endregion

        public TaskContext(DbContextOptions options) : base(options) { }
        
        public void SetWritable() => SetContextBehavior(ContextBehavior.Writable);
        public void SetReadOnly() => SetContextBehavior(ContextBehavior.Readonly);

        public virtual void SetContextBehavior(ContextBehavior behavior)
        {
            ChangeTracker.QueryTrackingBehavior = behavior.GetQueryTrackingBehavior();
            _contextBehavior = behavior;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;
            
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseQueryTrackingBehavior(_contextBehavior.GetQueryTrackingBehavior());
            optionsBuilder.ReplaceService<IEntityMaterializerSource, DateTimeMaterializerSource>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null || _efAssembly == null)
                return;
            
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseEntityTypeConfiguration(_efAssembly);
            modelBuilder.RemovePluralizingTableNameConvention();
        }
    }
}