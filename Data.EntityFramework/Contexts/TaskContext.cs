﻿using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Data.EntityFramework.Extensions;
using N17Solutions.Microphobia.Domain.Tasks;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class TaskContext : DbContext
    {
        private readonly Assembly _efAssembly = typeof(TaskContext).Assembly;
        
        #region Aggregate Roots
        public DbSet<TaskInfo> Tasks { get; set; }
        #endregion

        public TaskContext(DbContextOptions<TaskContext> options) : base(options) { }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
                return;
            
            base.OnConfiguring(optionsBuilder);

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