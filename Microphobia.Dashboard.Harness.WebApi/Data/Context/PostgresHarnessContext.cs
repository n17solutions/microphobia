using Microphobia.Dashboard.Harness.WebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Microphobia.Dashboard.Harness.WebApi.Data.Context
{
    public class PostgresHarnessContext : DbContext
    {
        public DbSet<HarnessEntity> HarnessEntities { get; set; }
        
        public PostgresHarnessContext(DbContextOptions options) : base(options) { }
    }
}