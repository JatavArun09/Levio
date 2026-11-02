using Levio.Modules.Identity.Application.DTOs.Auth;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Application.Services;
using Levio.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Levio.Modules.Identity.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "__Host-levio-refresh";
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);

            SetRefreshCookie(result.RefreshToken, result.ExpiresAt);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [EnableRateLimiting("authentication")]
        public async Task<IActionResult> RefreshToken()
        {
            var result = await _authService.RefreshTokenAsync(GetRefreshToken());
            SetRefreshCookie(result.RefreshToken, result.ExpiresAt);
            return Ok(result);
        }
        
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(GetRefreshToken());
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = "/", Secure = true, SameSite = SameSiteMode.Strict });

            return NoContent();
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(new { id = userId, email = User.FindFirstValue(ClaimTypes.Email), roles = User.FindAll(ClaimTypes.Role).Select(role => role.Value) });
        }

        private string GetRefreshToken() => Request.Cookies.TryGetValue(RefreshCookieName, out var token) ? token : string.Empty;

        private void SetRefreshCookie(string refreshToken, DateTime expiresAt) => Response.Cookies.Append(RefreshCookieName, refreshToken,
            new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/", Expires = new DateTimeOffset(expiresAt), IsEssential = true });
    }
}
