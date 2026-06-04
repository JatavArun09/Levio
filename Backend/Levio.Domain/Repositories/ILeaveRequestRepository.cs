
using Levio.Domain.Entities;

namespace Levio.Domain.Repositories
{
    public interface ILeaveRequestRepository
    {
        Task<LeaveRequest?> GetByIdAsync(long id);
        Task<List<LeaveRequest>> GetByUserIdAsync(long userId);
        Task<List<LeaveRequest>> GetPendingRequestsAsync();
    }
}
