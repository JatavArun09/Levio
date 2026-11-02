using Levio.Domain.Common;
using Levio.Domain.Enums;

namespace Levio.Domain.Entities
{
    public class LeaveType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public LeaveTypeCode Code { get; set; }

        public int DefaultDaysPerYear { get; set; }

        public bool IsActive { get; set; }

        public ICollection<LeaveBalance> LeaveBalances { get; set; }
            = new List<LeaveBalance>();

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
            = new List<LeaveRequest>();
    }
}