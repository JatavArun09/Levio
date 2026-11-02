using Levio.Infrastructure.Data;
using Levio.Modules.LeaveManagement.Domain.Entities;
using Levio.Modules.LeaveManagement.Domain.Enums;
using Levio.Modules.LeaveManagement.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.LeaveManagement.Repositories;

internal sealed class LeaveRequestRepository(ApplicationDbContext db) : ILeaveRequestRepository
{
    private IQueryable<LeaveRequest> Active => db.LeaveRequests.Where(r => !r.IsDeleted);
    public Task<LeaveRequest?> GetByIdAsync(long id) => Active.Include(r => r.LeaveType).SingleOrDefaultAsync(r => r.Id == id);
    public Task<List<LeaveRequest>> GetByUserIdAsync(long userId) => Active.AsNoTracking().Include(r => r.LeaveType)
        .Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).ToListAsync();
    public Task<List<LeaveRequest>> GetPendingRequestsAsync() => Active.AsNoTracking().Include(r => r.LeaveType)
        .Where(r => r.Status == LeaveStatus.Pending).ToListAsync();

    public async Task<(IReadOnlyList<LeaveRequest> Items, int TotalCount)> GetPageAsync(long userId, int pageNumber,
        int pageSize, LeaveStatus? status, CancellationToken ct)
    {
        var query = Active.AsNoTracking().Where(r => r.UserId == userId);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        var count = await query.CountAsync(ct);
        var items = await query.Include(r => r.LeaveType).OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, count);
    }

    public Task<bool> HasOverlapAsync(long userId, DateOnly startDate, DateOnly endDate, bool isHalfDay,
        HalfDayType? halfDayType, long? excludingId, CancellationToken ct)
        => Active.AnyAsync(r => r.UserId == userId && (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.Approved)
            && (!excludingId.HasValue || r.Id != excludingId.Value) && r.StartDate <= endDate && r.EndDate >= startDate
            && (!isHalfDay || !r.IsHalfDay || r.HalfDayType == halfDayType), ct);

    public async Task<IReadOnlyDictionary<long, decimal>> GetPendingDaysAsync(long userId, DateOnly yearStart,
        DateOnly yearEnd, CancellationToken ct) => await Active.AsNoTracking()
        .Where(r => r.UserId == userId && r.Status == LeaveStatus.Pending && r.StartDate >= yearStart && r.StartDate < yearEnd)
        .GroupBy(r => r.LeaveTypeId).Select(g => new { Type = g.Key, Days = g.Sum(r => r.NumberOfDays) })
        .ToDictionaryAsync(x => x.Type, x => x.Days, ct);

    public async Task AddAsync(LeaveRequest request, CancellationToken ct) => await db.LeaveRequests.AddAsync(request, ct);
}
