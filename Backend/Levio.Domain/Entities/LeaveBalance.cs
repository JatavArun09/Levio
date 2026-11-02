using Levio.Domain.Common;

namespace Levio.Domain.Entities
{
    public class LeaveBalance : BaseEntity
    {
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        public long LeaveTypeId { get; set; }

        public LeaveType LeaveType { get; set; } = null!;

        public int Year { get; set; }

        public decimal AllocatedDays { get; set; }

        public decimal UsedDays { get; set; }

        public decimal RemainingDays { get; set; }
    }
}