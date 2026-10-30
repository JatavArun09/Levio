using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.LeaveManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Levio.Infrastructure.LeaveManagement.Configurations
{
    public class LeaveRequestConfiguration :IEntityTypeConfiguration<LeaveRequest>
    {
        public void Configure(EntityTypeBuilder<LeaveRequest> builder)
        {
            builder.ToTable("LeaveRequests");
            
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Reason)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.RejectionComment)
                .HasMaxLength(1000);

            builder.Property(x => x.NumberOfDays)
                .HasPrecision(5, 2);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.StartDate);

            builder.HasIndex(x => x.EndDate);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LeaveType)
                .WithMany(x => x.LeaveRequests)
                .HasForeignKey(x => x.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
