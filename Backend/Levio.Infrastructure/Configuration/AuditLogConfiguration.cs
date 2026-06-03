using Levio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.Configuration
{
    public class AuditLogConfiguration:IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Action)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EntityName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IpAddress)
                .HasMaxLength(100);

            builder.Property(x => x.CorrelationId)
                .HasMaxLength(100);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
