namespace Levio.Modules.Identity.Contracts;

public sealed record EmployeeInfo(long Id, bool IsActive, long DepartmentId, long? DepartmentManagerId);

public interface IEmployeeDirectory
{
    Task<EmployeeInfo?> GetAsync(long userId, CancellationToken cancellationToken = default);
}
