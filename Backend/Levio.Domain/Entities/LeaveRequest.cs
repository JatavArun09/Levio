using Levio.Domain.Common;
using Levio.Domain.Enums;

namespace Levio.Domain.Entities
{
    public class LeaveRequest : BaseEntity
    {
        public long UserId { get; set; }

        public User User { get; set; } = null!;

        public long LeaveTypeId { get; set; }

        public LeaveType LeaveType { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public decimal NumberOfDays { get; set; }

        public bool IsHalfDay { get; set; }

        public HalfDayType? HalfDayType { get; set; }

        public string Reason { get; set; } = string.Empty;

        public LeaveStatus Status { get; set; }

        public string? RejectionComment { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public long? ApprovedByUserId { get; set; }
    }
}