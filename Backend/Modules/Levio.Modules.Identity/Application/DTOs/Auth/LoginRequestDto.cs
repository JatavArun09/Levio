using System.ComponentModel.DataAnnotations;

namespace Levio.Modules.Identity.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(256, MinimumLength = 12)]
        public string Password { get; set; } = string.Empty;
    }
}
