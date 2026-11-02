using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Application.DTOs.Auth
{
    public class UserInfoDto
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
