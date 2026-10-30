using Levio.Modules.LeaveManagement.Domain.Entities;


namespace Levio.Modules.LeaveManagement.Domain.Repositories
{
    public interface ILeaveRequestRepository
    {
        Task<LeaveRequest?> GetByIdAsync(long id);
        Task<List<LeaveRequest>> GetByUserIdAsync(long userId);
        Task<List<LeaveRequest>> GetPendingRequestsAsync();
        Task<(IReadOnlyList<LeaveRequest> Items, int TotalCount)> GetPageAsync(long userId, int pageNumber,
            int pageSize, Levio.Modules.LeaveManagement.Domain.Enums.LeaveStatus? status, CancellationToken cancellationToken);
        Task<bool> HasOverlapAsync(long userId, DateOnly startDate, DateOnly endDate, bool isHalfDay,
            Levio.Modules.LeaveManagement.Domain.Enums.HalfDayType? halfDayType, long? excludingId, CancellationToken cancellationToken);
        Task<IReadOnlyDictionary<long, decimal>> GetPendingDaysAsync(long userId, DateOnly yearStart,
            DateOnly yearEnd, CancellationToken cancellationToken);
        Task AddAsync(LeaveRequest request, CancellationToken cancellationToken);
    }
}
