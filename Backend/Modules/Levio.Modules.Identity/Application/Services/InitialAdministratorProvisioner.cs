using System.ComponentModel.DataAnnotations;
using Levio.Modules.Identity.Application.Configurations;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.Identity.Domain.Repositories;
using Levio.SharedKernel.Persistence;

namespace Levio.Modules.Identity.Application.Services;

internal sealed class InitialAdministratorProvisioner(
    IUserRepository users,
    IRepository<Role> roles,
    IRepository<UserRole> userRoles,
    IRepository<Department> departments,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ProvisionAsync(InitialAdministratorSettings settings)
    {
        Validate(settings);

        var hrAdminRole = (await roles.FindAsync(role => role.Name == "HRAdmin" && !role.IsDeleted)).SingleOrDefault()
            ?? throw new InvalidOperationException("The HRAdmin role must exist before provisioning an administrator.");

        var hrAdminAssignments = await userRoles.FindAsync(assignment => assignment.RoleId == hrAdminRole.Id && !assignment.IsDeleted);
        foreach (var assignment in hrAdminAssignments)
        {
            var assignedUser = await users.GetUserWithRolesAsync(assignment.UserId);
            if (assignedUser is { ISActive: true, IsDeleted: false }) return false;
        }

        var email = settings.Email.Trim();
        if (await users.GetByEmailAsync(email) is not null)
            throw new InvalidOperationException("The configured initial administrator email already belongs to a user.");
        if (await users.GetByEmployeeCodeAsync(settings.EmployeeCode.Trim()) is not null)
            throw new InvalidOperationException("The configured initial administrator employee code already belongs to a user.");
        if (await departments.GetByIdAsync(settings.DepartmentId) is not { IsDeleted: false })
            throw new InvalidOperationException("The configured initial administrator department does not exist or is inactive.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            EmployeeCode = settings.EmployeeCode.Trim(),
            FirstName = settings.FirstName.Trim(),
            LastName = settings.LastName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(settings.Password),
            ISActive = true,
            DepartmentId = settings.DepartmentId,
            DateOfJoining = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.UserRoles.Add(new UserRole
        {
            RoleId = hrAdminRole.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        await users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    private static void Validate(InitialAdministratorSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password) ||
            string.IsNullOrWhiteSpace(settings.EmployeeCode) || string.IsNullOrWhiteSpace(settings.FirstName) ||
            string.IsNullOrWhiteSpace(settings.LastName) || settings.DepartmentId <= 0)
            throw new InvalidOperationException("InitialAdministrator requires email, password, employee code, name and a valid department ID.");
        if (!new EmailAddressAttribute().IsValid(settings.Email))
            throw new InvalidOperationException("InitialAdministrator email is invalid.");
        if (settings.Password.Length < 14 || !settings.Password.Any(char.IsUpper) || !settings.Password.Any(char.IsLower) ||
            !settings.Password.Any(char.IsDigit) || !settings.Password.Any(character => !char.IsLetterOrDigit(character)))
            throw new InvalidOperationException("InitialAdministrator password must be at least 14 characters and contain uppercase, lowercase, digit and symbol characters.");
    }
}
