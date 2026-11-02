using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Levio.Modules.Identity.Application.Configurations;
using Levio.Modules.Identity.Application.DTOs.Auth;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.Identity.Domain.Repositories;
using Levio.SharedKernel.Persistence;
using Levio.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using static ArchitectureChecks;

internal static class AuthenticationChecks
{
    private const string Secret = "Levio-test-only-signing-key-12345678901234567890";
    private static readonly Dictionary<string, string?> Settings = new()
    {
        ["JwtSettings:Issuer"] = "Levio.Tests", ["JwtSettings:Audience"] = "Levio.Tests.Users",
        ["JwtSettings:SecretKey"] = Secret, ["JwtSettings:AccessTokenMinutes"] = "23",
        ["JwtSettings:RefreshTokenDays"] = "3", ["HttpsRedirection:Enabled"] = "false"
    };

    public static async Task RunAsync()
    {
        var users = new Users();
        var tokens = new Tokens();
        var commits = new Commits();
        await using var app = CreateHost(users, tokens, commits);
        using (var scope = app.Services.CreateScope())
        {
            users.User.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash("test-password");
        }
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
        using (var rootResponse = await client.GetAsync("/"))
        {
            Check(rootResponse.IsSuccessStatusCode && rootResponse.RequestMessage!.RequestUri!.AbsolutePath.StartsWith("/swagger"),
                "Development root must lead to Swagger.");
        }
        await Status(client.GetAsync("/api/Auth/testing"), HttpStatusCode.Unauthorized);
        await Status(client.GetAsync("/api/Test/testing"), HttpStatusCode.Unauthorized);
        await Status(client.GetAsync("/api/Test/admin"), HttpStatusCode.Unauthorized);
        await Status(client.GetAsync("/api/WeatherForecast"), HttpStatusCode.OK);
        using (var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json"))
        {
            Check(swaggerResponse.IsSuccessStatusCode, "Swagger endpoint missing.");
            using var swagger = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStringAsync());
            foreach (var route in new[] { "/api/Auth/login", "/api/Auth/refresh-token", "/api/Auth/logout",
                "/api/Auth/testing", "/api/Test/testing", "/api/Test/admin", "/api/WeatherForecast",
                "/api/v1/leave-requests", "/api/v1/leave-requests/{id}", "/api/v1/leave-balances",
                "/api/v1/leave-balances/{userId}", "/api/v1/leave-types", "/api/v1/leave-types/{id}" })
                Check(swagger.RootElement.GetProperty("paths").TryGetProperty(route, out _), $"Missing API route {route}.");
        }
        using (var invalid = await client.PostAsJsonAsync("/api/Auth/login", new { Email = "missing@example.com", Password = "wrong" }))
        {
            Check(invalid.StatusCode == HttpStatusCode.Unauthorized, "Unknown user must return 401.");
            using var error = JsonDocument.Parse(await invalid.Content.ReadAsStringAsync());
            Check(error.RootElement.GetProperty("StatusCode").GetInt32() == 401, "Exception middleware response missing.");
        }
        await Status(client.PostAsJsonAsync("/api/Auth/login", new { users.User.Email, Password = "wrong" }), HttpStatusCode.Unauthorized);
        var beforeLogin = DateTime.UtcNow;
        using var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new { users.User.Email, Password = "test-password" });
        Check(loginResponse.StatusCode == HttpStatusCode.OK, "Login failed.");
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>())!;
        Check(login.User.Id == 42 && login.User.Email == users.User.Email && login.User.EmployeeCode == "EMP042"
            && login.User.FullName == "Test Employee" && login.User.Roles.SequenceEqual(new[] { "Employee" }), "Login user DTO changed.");
        Check(Math.Abs((login.ExpiresAt - beforeLogin.AddMinutes(23)).TotalSeconds) < 5, "Login response ignores configured lifetime.");
        Check(Math.Abs((tokens.Items.Single().ExpiresAt - beforeLogin.AddDays(3)).TotalSeconds) < 5, "Refresh lifetime ignores configuration.");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        Check(jwt.Subject == "42" && jwt.Claims.Any(c => c.Type == "employeeCode" && c.Value == "EMP042"), "JWT claims changed.");
        Check(Math.Abs((jwt.ValidTo - login.ExpiresAt).TotalSeconds) < 2, "JWT expiry differs from response expiry.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        await Status(client.GetAsync("/api/Auth/testing"), HttpStatusCode.OK);
        await Status(client.GetAsync("/api/Test/testing"), HttpStatusCode.OK);
        await Status(client.GetAsync("/api/Test/admin"), HttpStatusCode.Forbidden);
        using (var scope = app.Services.CreateScope())
        {
            users.User.UserRoles.Single().Role.Name = "HRAdmin";
            var adminToken = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GenerateAccessToken(users.User);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            await Status(client.GetAsync("/api/Test/admin"), HttpStatusCode.OK);
        }
        foreach (var badToken in new[] { "malformed", MakeToken(issuer: "wrong"), MakeToken(audience: "wrong"),
            MakeToken(secret: Secret + "wrong"), MakeToken(expires: DateTime.UtcNow.AddMinutes(-10)) })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badToken);
            await Status(client.GetAsync("/api/Auth/testing"), HttpStatusCode.Unauthorized);
        }
        client.DefaultRequestHeaders.Authorization = null;
        using var refreshedResponse = await client.PostAsJsonAsync("/api/Auth/refresh-token", new { login.RefreshToken });
        Check(refreshedResponse.StatusCode == HttpStatusCode.OK, "Refresh failed.");
        var refreshed = (await refreshedResponse.Content.ReadFromJsonAsync<LoginResponseDto>())!;
        Check(refreshed.RefreshToken != login.RefreshToken, "Refresh did not rotate token.");
        Check(Math.Abs((refreshed.ExpiresAt - DateTime.UtcNow.AddMinutes(23)).TotalSeconds) < 5, "Refresh response ignores lifetime.");
        await Status(client.PostAsJsonAsync("/api/Auth/refresh-token", new { login.RefreshToken }), HttpStatusCode.Unauthorized);
        await Status(client.PostAsJsonAsync("/api/Auth/logout", new { refreshed.RefreshToken }), HttpStatusCode.NoContent);
        Check(tokens.Items.Single().IsRevoked && tokens.Items.Single().RevokedAt.HasValue, "Logout did not persist revocation.");
        await Status(client.PostAsJsonAsync("/api/Auth/refresh-token", new { refreshed.RefreshToken }), HttpStatusCode.Unauthorized);
        await Status(client.PostAsJsonAsync("/api/Auth/logout", new { RefreshToken = "unknown" }), HttpStatusCode.NoContent);
        tokens.Items.Add(new RefreshToken { TokenHash = "expired", UserId = 42, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        await Status(client.PostAsJsonAsync("/api/Auth/refresh-token", new { RefreshToken = "expired" }), HttpStatusCode.Unauthorized);
        tokens.Items.Add(new RefreshToken { TokenHash = "revoked-at", UserId = 42, ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow });
        await Status(client.PostAsJsonAsync("/api/Auth/refresh-token", new { RefreshToken = "revoked-at" }), HttpStatusCode.Unauthorized);
        Check(commits.Count == 3, "Unexpected successful auth transaction count.");
        await app.StopAsync();
        Console.WriteLine("PASS: Real HTTP routes, Swagger, login, JWT validation, role authorization, refresh rotation, logout and failures.");

        foreach (var invalid in new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "", ["JwtSettings:Audience"] = "", ["JwtSettings:SecretKey"] = "short",
            ["JwtSettings:AccessTokenMinutes"] = "0", ["JwtSettings:RefreshTokenDays"] = "0"
        })
        {
            await using var invalidHost = CreateHost(new Users(), new Tokens(), new Commits(), invalid);
            try
            {
                await invalidHost.StartAsync();
                throw new Exception($"Invalid JWT setting accepted: {invalid.Key}");
            }
            catch (OptionsValidationException) { }
        }
        Console.WriteLine("PASS: Invalid JWT configuration rejected at startup.");
    }

    private static WebApplication CreateHost(Users users, Tokens tokens, Commits commits, KeyValuePair<string, string?>? invalid = null)
        => ApiHost.Build(["--environment", "Development"], builder =>
        {
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(Settings);
            if (invalid.HasValue) builder.Configuration.AddInMemoryCollection([invalid.Value]);
            builder.Services.RemoveAll<IUserRepository>();
            builder.Services.RemoveAll<IRefreshTokenRepository>();
            builder.Services.RemoveAll<IUnitOfWork>();
            builder.Services.AddSingleton<IUserRepository>(users);
            builder.Services.AddSingleton<IRefreshTokenRepository>(tokens);
            builder.Services.AddSingleton<IUnitOfWork>(commits);
        });

    private static string MakeToken(string issuer = "Levio.Tests", string audience = "Levio.Tests.Users", string secret = Secret, DateTime? expires = null)
        => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience,
            [new Claim(ClaimTypes.NameIdentifier, "42")], expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256)));

    private static async Task Status(Task<HttpResponseMessage> request, HttpStatusCode expected)
    {
        using var response = await request;
        Check(response.StatusCode == expected, $"{response.RequestMessage!.RequestUri!.AbsolutePath}: expected {expected}, got {response.StatusCode}.");
    }

    private sealed class Users : IUserRepository
    {
        public User User { get; } = new()
        {
            Id = 42, Email = "employee@example.com", EmployeeCode = "EMP042", FirstName = "Test", LastName = "Employee",
            UserRoles = [new UserRole { UserId = 42, RoleId = 1, Role = new Role { Id = 1, Name = "Employee" } }]
        };
        public async Task<User?> GetByEmailAsync(string email) { await Task.Yield(); return email == User.Email ? User : null; }
        public Task<User?> GetByEmployeeCodeAsync(string employeeCode) => Task.FromResult(employeeCode == User.EmployeeCode ? User : null);
        public Task<User?> GetUserWithRolesAsync(long userId) => Task.FromResult(userId == User.Id ? User : null);
        public Task<bool> ExistsByEmailAsync(string email) => Task.FromResult(email == User.Email);
    }

    private sealed class Tokens : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = [];
        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) => Task.FromResult(Items.SingleOrDefault(t => t.TokenHash == tokenHash));
        public Task<RefreshToken?> GetByTokenAsync(string token) => GetByTokenHashAsync(token);
        public Task AddAsync(RefreshToken refreshToken) { Items.Add(refreshToken); return Task.CompletedTask; }
        public Task UpdateAsync(RefreshToken refreshToken) => Task.CompletedTask;
    }

    private sealed class Commits : IUnitOfWork
    {
        public int Count { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Count++; return Task.FromResult(1); }
    }
}
