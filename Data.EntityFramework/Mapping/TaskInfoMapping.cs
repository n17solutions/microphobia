using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Domain.Tasks;

namespace N17Solutions.Microphobia.Data.EntityFramework.Mapping
{
    public class TaskInfoMapping : IEntityTypeConfiguration<TaskInfo>
    {
        public void Configure(EntityTypeBuilder<TaskInfo> builder)
        {
            #region Table

            builder.ToTable(nameof(TaskInfo), Schema.MicrophobiaSchemaName);

            #endregion
        }
    }
}