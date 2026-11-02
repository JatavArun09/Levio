using Levio.Modules.Identity.Application.Configurations;
using Levio.Modules.Identity.Application.DTOs.Auth;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.Identity.Domain.Repositories;
using Levio.SharedKernel.Persistence;

using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Levio.Modules.Identity.Application.Services
{
    internal sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;


        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenService jwtTokenService,
            IUnitOfWork unitOfWork,
            IOptions<JwtSettings> jwtSettings
            )
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var email = request.Email?.Trim();
            var user = string.IsNullOrWhiteSpace(email) ? null : await _userRepository.GetByEmailAsync(email);
            if (user is null || !user.ISActive || user.IsDeleted ||
                !_passwordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid email or password.");

            user = await _userRepository.GetUserWithRolesAsync(user.Id);
            if (user is null || !user.ISActive || user.IsDeleted)
                throw new UnauthorizedAccessException("Invalid email or password.");

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashRefreshToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays),
                CreatedAt = DateTime.UtcNow
            };
            await _refreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
                User = ToUserInfo(user)
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var token = await _refreshTokenRepository.GetByTokenHashAsync(HashRefreshToken(refreshToken));

            if (token is null)
                return;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(HashRefreshToken(refreshToken));

            if (existingToken is null || existingToken.IsRevoked || existingToken.RevokedAt.HasValue)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            if (existingToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Refresh token has expired.");
            }

            var user = await _userRepository.GetUserWithRolesAsync(existingToken.UserId);

            if (user is null || !user.ISActive || user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user);

            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            existingToken.TokenHash = HashRefreshToken(newRefreshToken);
            existingToken.ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            await _refreshTokenRepository.UpdateAsync(existingToken);

            await _unitOfWork.SaveChangesAsync();

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
                User = ToUserInfo(user)
            };
        }
        private static UserInfoDto ToUserInfo(User user) => new()
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email,
            EmployeeCode = user.EmployeeCode,
            Roles = user.UserRoles.Select(userRole => userRole.Role.Name).ToList()
        };

        private static string HashRefreshToken(string refreshToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
