using Levio.Modules.LeaveManagement.Domain.Entities;

namespace Levio.Modules.LeaveManagement.Domain.Repositories;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetAsync(long userId, long leaveTypeId, int year, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveBalance>> GetForYearAsync(long userId, int year, CancellationToken cancellationToken);
}

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveType>> GetActiveAsync(CancellationToken cancellationToken);
}

public interface ILeaveWriteScope
{
    Task<T> ExecuteAsync<T>(long userId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
