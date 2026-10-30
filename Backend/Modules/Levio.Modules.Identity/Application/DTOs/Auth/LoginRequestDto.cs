using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.Identity.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
