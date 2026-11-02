using Levio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Levio.Infrastructure.Configurations
{
    public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
    {
        public void Configure(EntityTypeBuilder<LeaveBalance> builder)
        {
            builder.ToTable("LeaveBalances");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AllocatedDays)
                .HasPrecision(5, 2);

            builder.Property(x => x.UsedDays)
                .HasPrecision(5, 2);

            builder.Property(x => x.RemainingDays)
                .HasPrecision(5, 2);

            builder.HasIndex(x =>
                new { x.UserId, x.LeaveTypeId, x.Year })
                .IsUnique();

            builder.HasOne(x => x.User)
                .WithMany(x => x.LeaveBalances)
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.LeaveType)
                .WithMany(x => x.LeaveBalances)
                .HasForeignKey(x => x.LeaveTypeId);
        }
    }
}
