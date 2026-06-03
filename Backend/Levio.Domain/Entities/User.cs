using Levio.Domain.Common;
using Levio.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Domain.Entities
{
    public class User : BaseEntity
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool ISActive { get; set; } = true;
        public DateTime DateOfJoining { get; set; }
        public long DepartmentId { get; set; }
        public Department Department { get; set; } = null!;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
        public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();


    }
}
