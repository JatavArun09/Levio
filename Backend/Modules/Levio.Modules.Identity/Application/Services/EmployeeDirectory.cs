using Levio.Modules.Identity.Contracts;
using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.Identity.Domain.Repositories;
using Levio.SharedKernel.Persistence;

namespace Levio.Modules.Identity.Application.Services;

internal sealed class EmployeeDirectory(IUserRepository users, IRepository<Department> departments) : IEmployeeDirectory
{
    public async Task<EmployeeInfo?> GetAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.GetUserWithRolesAsync(userId);
        if (user is null) return null;
        var department = await departments.GetByIdAsync(user.DepartmentId);
        return new EmployeeInfo(user.Id, user.ISActive && !user.IsDeleted && department is { IsDeleted: false },
            user.DepartmentId, department?.ManagerId);
    }
}
