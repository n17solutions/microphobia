using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using N17Solutions.Microphobia.Data.EntityFramework.Conventions;
using N17Solutions.Microphobia.Domain.Clients;

namespace N17Solutions.Microphobia.Data.EntityFramework.Mapping
{
    public class QueueRunnerMapping  : IEntityTypeConfiguration<QueueRunner>
    {
        public void Configure(EntityTypeBuilder<QueueRunner> builder)
        {
            #region Table

            builder.ToTable(nameof(QueueRunner), Schema.MicrophobiaSchemaName);

            #endregion
        }
        
    }
}