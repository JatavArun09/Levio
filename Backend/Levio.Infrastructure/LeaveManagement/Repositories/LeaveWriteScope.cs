using System.Data;
using Levio.Infrastructure.Data;
using Levio.Modules.LeaveManagement.Domain.Repositories;
using Levio.SharedKernel.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Levio.Infrastructure.LeaveManagement.Repositories;

internal sealed class LeaveWriteScope(ApplicationDbContext db) : ILeaveWriteScope
{
    public async Task<T> ExecuteAsync<T>(long userId, Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Transaction-owned SQL Server lock serializes all leave writes for this employee,
        // including on other API instances. Parameters never become SQL command text.
        var result = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = $"Levio:Leave:{userId}" };
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @Result = sys.sp_getapplock @Resource=@Resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000;",
            new object[] { result, resource }, ct);
        if ((int)result.Value < 0) throw new RequestConflictException("Leave is being updated. Retry the request.");
        var response = await operation(ct);
        await transaction.CommitAsync(ct);
        return response;
    }
}
