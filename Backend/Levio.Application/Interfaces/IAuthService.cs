using Levio.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
    }
}
