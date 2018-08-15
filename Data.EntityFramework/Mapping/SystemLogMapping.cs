using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Domain.Logs;

namespace N17Solutions.Microphobia.Data.EntityFramework.Mapping
{
    public class SystemLogMapping : IEntityTypeConfiguration<SystemLog>
    {
        public void Configure(EntityTypeBuilder<SystemLog> builder)
        {
            #region Table

            builder.ToTable(nameof(SystemLog), Schema.MicrophobiaSchemaName);

            #endregion
        }
    }
}