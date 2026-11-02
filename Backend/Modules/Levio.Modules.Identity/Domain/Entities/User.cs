using Levio.SharedKernel.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.Identity.Domain.Entities
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
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();


    }
}
