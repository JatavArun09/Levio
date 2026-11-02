using Levio.Modules.Identity.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Modules.Identity.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }
}
