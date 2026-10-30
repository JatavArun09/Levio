using Levio.Modules.LeaveManagement.Domain.Enums;
using MediatR;

namespace Levio.Modules.LeaveManagement.Application;

public sealed record SaveLeaveRequest(LeaveActor Actor, long? Id, LeaveRequestInput Input) : IRequest<LeaveRequestDto>;
public sealed record CancelLeaveRequest(LeaveActor Actor, long Id) : IRequest<Unit>;
public sealed record GetLeaveRequest(LeaveActor Actor, long Id) : IRequest<LeaveRequestDto>;
public sealed record GetLeaveHistory(LeaveActor Actor, int PageNumber = 1, int PageSize = 10,
    LeaveStatus? Status = null) : IRequest<Page<LeaveRequestDto>>;
public sealed record GetLeaveBalances(LeaveActor Actor, long? UserId = null, int? Year = null) : IRequest<IReadOnlyList<LeaveBalanceDto>>;
public sealed record GetLeaveTypes(LeaveActor Actor) : IRequest<IReadOnlyList<LeaveTypeDto>>;
public sealed record GetLeaveType(LeaveActor Actor, long Id) : IRequest<LeaveTypeDto>;
