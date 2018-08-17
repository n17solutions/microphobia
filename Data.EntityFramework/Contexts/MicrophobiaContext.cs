using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Data.EntityFramework.Extensions;
using N17Solutions.Microphobia.Domain.Model;

namespace N17Solutions.Microphobia.Data.EntityFramework.Contexts
{
    public class MicrophobiaContext : DbContext
    {
        private readonly Assembly _efAssembly;

        public MicrophobiaContext(DbContextOptions options, Assembly efAssembly) : base(options)
        {
            _efAssembly = efAssembly;
        }
        
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnSavingChanges();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override int SaveChanges()
        {
            OnSavingChanges();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            OnSavingChanges();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            OnSavingChanges();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnSavingChanges()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                var entryClosure = entry;

                if (entryClosure.State != EntityState.Added && entryClosure.State != EntityState.Modified)
                    continue;

                if (!(entryClosure.Entity is ITimestampedEntity entity))
                    continue;

                entity.DateLastUpdated = now;

                if (entryClosure.State == EntityState.Added)
                    entity.DateCreated = now;
            }
        }
        
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