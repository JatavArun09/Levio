using Levio.Infrastructure.Data;
using Levio.Modules.LeaveManagement.Domain.Entities;
using Levio.Modules.LeaveManagement.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.LeaveManagement.Repositories;

internal sealed class LeaveBalanceRepository(ApplicationDbContext db) : ILeaveBalanceRepository
{
    public Task<LeaveBalance?> GetAsync(long userId, long leaveTypeId, int year, CancellationToken ct)
        => db.LeaveBalances.SingleOrDefaultAsync(b => !b.IsDeleted && b.UserId == userId && b.LeaveTypeId == leaveTypeId && b.Year == year, ct);
    public async Task<IReadOnlyList<LeaveBalance>> GetForYearAsync(long userId, int year, CancellationToken ct)
        => await db.LeaveBalances.AsNoTracking().Where(b => !b.IsDeleted && b.UserId == userId && b.Year == year).ToListAsync(ct);
}

internal sealed class LeaveTypeRepository(ApplicationDbContext db) : ILeaveTypeRepository
{
    public Task<LeaveType?> GetAsync(long id, CancellationToken ct)
        => db.LeaveTypes.SingleOrDefaultAsync(t => !t.IsDeleted && t.Id == id, ct);
    public async Task<IReadOnlyList<LeaveType>> GetActiveAsync(CancellationToken ct)
        => await db.LeaveTypes.AsNoTracking().Where(t => !t.IsDeleted && t.IsActive).OrderBy(t => t.Id).ToListAsync(ct);
}
