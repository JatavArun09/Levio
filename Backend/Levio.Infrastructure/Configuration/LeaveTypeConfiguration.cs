using Levio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.Configuration
{
    public class LeaveTypeConfiguration:IEntityTypeConfiguration<LeaveType>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<LeaveType> builder)
        {
            builder.ToTable("LeaveTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => x.Code)
                .IsUnique();
        }

    }
}
