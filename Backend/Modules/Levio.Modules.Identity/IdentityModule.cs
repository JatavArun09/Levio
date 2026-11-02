using System.Text;
using Levio.Modules.Identity.Application.Configurations;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Application.Services;
using Levio.Modules.Identity.Contracts;
using Levio.SharedKernel.Security;
using Levio.Modules.Identity.Authentication;
using Levio.Modules.Identity.Presentation.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Levio.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("JwtSettings"))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "JWT issuer is required.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "JWT audience is required.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey) && Encoding.UTF8.GetByteCount(s.SecretKey) >= 32,
                "JWT signing key must contain at least 32 UTF-8 bytes.")
            .Validate(s => s.AccessTokenMinutes > 0 && s.RefreshTokenDays > 0, "JWT lifetimes must be positive.")
            .ValidateOnStart();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, settings) =>
            {
                var jwt = settings.Value;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true,
                    ValidateIssuerSigningKey = true, ValidIssuer = jwt.Issuer, ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey))
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.RequireAuthenticated, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(AuthorizationPolicies.RequireEmployee, policy => policy.RequireRole("Employee", "Manager", "HRAdmin"));
            options.AddPolicy(AuthorizationPolicies.RequireManager, policy => policy.RequireRole("Manager", "HRAdmin"));
            options.AddPolicy(AuthorizationPolicies.RequireHRAdmin, policy => policy.RequireRole("HRAdmin"));
        });
        return services;
    }
}
