using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace N17Solutions.Microphobia.Data.EntityFramework.Conventions
{
    public static class ModelBuilderExtensions
    {
        public static void RemovePluralizingTableNameConvention(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                entity.Relational().TableName = entity.DisplayName();
        }
    }
}