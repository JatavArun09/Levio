using Levio.Modules.Identity.Contracts;
using Levio.Modules.LeaveManagement.Domain.Entities;
using Levio.Modules.LeaveManagement.Domain.Enums;
using Levio.Modules.LeaveManagement.Domain.Exceptions;
using Levio.Modules.LeaveManagement.Domain.Repositories;
using Levio.SharedKernel.Domain;
using Levio.SharedKernel.Persistence;
using MediatR;
using Microsoft.Extensions.Options;

namespace Levio.Modules.LeaveManagement.Application;

internal sealed class LeaveHandlers(ILeaveRequestRepository requests, ILeaveBalanceRepository balances,
    ILeaveTypeRepository types, ILeaveWriteScope writes, IEmployeeDirectory employees, IUnitOfWork unitOfWork,
    LeaveCalendar calendar, IOptions<LeaveCalendarOptions> options, TimeProvider clock) :
    IRequestHandler<SaveLeaveRequest, LeaveRequestDto>, IRequestHandler<CancelLeaveRequest, Unit>,
    IRequestHandler<GetLeaveRequest, LeaveRequestDto>, IRequestHandler<GetLeaveHistory, Page<LeaveRequestDto>>,
    IRequestHandler<GetLeaveBalances, IReadOnlyList<LeaveBalanceDto>>, IRequestHandler<GetLeaveTypes, IReadOnlyList<LeaveTypeDto>>,
    IRequestHandler<GetLeaveType, LeaveTypeDto>
{
    public async Task<LeaveRequestDto> Handle(SaveLeaveRequest command, CancellationToken ct)
    {
        await RequireActive(command.Actor, ct);
        return await writes.ExecuteAsync(command.Actor.UserId, async token =>
        {
            var input = command.Input;
            var existing = command.Id.HasValue ? await OwnedRequest(command.Id.Value, command.Actor.UserId) : null;
            if (existing is not null && existing.Status != LeaveStatus.Draft)
                throw new RequestConflictException("Only draft requests can be updated.");
            var type = await types.GetAsync(input.LeaveTypeId, token);
            if (type is null || !type.IsActive || type.IsDeleted) throw new BusinessRuleException("Select an active leave type.");
            if (input.StartDate < calendar.Today) throw new BusinessRuleException("Leave cannot start in the past.");
            var year = calendar.Year(input.StartDate);
            if (year is < 1900 or > 9998) throw new BusinessRuleException("Leave year is outside the supported range.");
            if (year != calendar.Year(input.EndDate))
                throw new BusinessRuleException("Split requests that cross the leave-year boundary into separate applications.");
            if (input.IsHalfDay && type.Code is not LeaveTypeCode.CL and not LeaveTypeCode.SL)
                throw new BusinessRuleException("Half-day leave is available only for CL and SL.");
            var days = calendar.Count(input.StartDate, input.EndDate, type.Code, input.IsHalfDay);
            if (days <= 0) throw new BusinessRuleException("The selected dates contain no eligible leave days.");
            if (days > options.Value.MaxRequestDays) throw new BusinessRuleException("Leave exceeds the maximum request duration.");
            if (type.Code == LeaveTypeCode.CL && days > options.Value.MaxCasualLeaveDays)
                throw new BusinessRuleException("Casual leave exceeds the maximum consecutive days.");
            if (!input.SaveAsDraft)
            {
                if (type.Code == LeaveTypeCode.EL && input.StartDate < calendar.Today.AddDays(options.Value.EarnedLeaveNoticeDays))
                    throw new BusinessRuleException("Earned leave requires advance notice.");
                if (await requests.HasOverlapAsync(command.Actor.UserId, input.StartDate, input.EndDate,
                    input.IsHalfDay, input.HalfDayType, command.Id, token))
                    throw new RequestConflictException("An active leave request already overlaps these dates.");
                var balance = await balances.GetAsync(command.Actor.UserId, type.Id, year, token);
                if (balance is null || balance.RemainingDays < days)
                    throw new LeaveBalanceException("Insufficient available leave balance. Ask HR to allocate your balance.");
                balance.RemainingDays -= days;
                balance.UpdatedAt = clock.GetUtcNow().UtcDateTime;
            }
            var request = existing ?? new LeaveRequest { UserId = command.Actor.UserId, CreatedAt = clock.GetUtcNow().UtcDateTime };
            request.LeaveTypeId = type.Id;
            request.LeaveType = type;
            request.StartDate = input.StartDate;
            request.EndDate = input.EndDate;
            request.IsHalfDay = input.IsHalfDay;
            request.HalfDayType = input.HalfDayType;
            request.NumberOfDays = days;
            request.Reason = input.Reason.Trim();
            request.Status = input.SaveAsDraft ? LeaveStatus.Draft : LeaveStatus.Pending;
            request.UpdatedAt = clock.GetUtcNow().UtcDateTime;
            if (existing is null) await requests.AddAsync(request, token);
            await unitOfWork.SaveChangesAsync(token);
            return Map(request);
        }, ct);
    }

    public async Task<Unit> Handle(CancelLeaveRequest command, CancellationToken ct)
    {
        await RequireActive(command.Actor, ct);
        return await writes.ExecuteAsync(command.Actor.UserId, async token =>
        {
            var request = await OwnedRequest(command.Id, command.Actor.UserId);
            if (request.Status is not LeaveStatus.Draft and not LeaveStatus.Pending)
                throw new RequestConflictException("Only Pending or Draft requests can be cancelled.");
            if (request.Status == LeaveStatus.Pending)
            {
                var balance = await balances.GetAsync(request.UserId, request.LeaveTypeId, calendar.Year(request.StartDate), token)
                    ?? throw new RequestConflictException("The leave balance is missing; contact HR before cancellation.");
                balance.RemainingDays += request.NumberOfDays;
                balance.UpdatedAt = clock.GetUtcNow().UtcDateTime;
            }
            request.Status = LeaveStatus.Cancelled;
            request.UpdatedAt = clock.GetUtcNow().UtcDateTime;
            await unitOfWork.SaveChangesAsync(token);
            return Unit.Value;
        }, ct);
    }

    public async Task<LeaveRequestDto> Handle(GetLeaveRequest query, CancellationToken ct)
    {
        await RequireActive(query.Actor, ct);
        var request = await requests.GetByIdAsync(query.Id);
        if (request is null || request.IsDeleted) throw new KeyNotFoundException("Leave request not found.");
        await RequireAccess(query.Actor, request.UserId, ct);
        return Map(request);
    }

    public async Task<Page<LeaveRequestDto>> Handle(GetLeaveHistory query, CancellationToken ct)
    {
        await RequireActive(query.Actor, ct);
        var page = await requests.GetPageAsync(query.Actor.UserId, query.PageNumber, query.PageSize, query.Status, ct);
        return new Page<LeaveRequestDto>(page.Items.Select(Map).ToList(), new PageMeta(query.PageNumber, query.PageSize, page.TotalCount));
    }

    public async Task<IReadOnlyList<LeaveBalanceDto>> Handle(GetLeaveBalances query, CancellationToken ct)
    {
        await RequireActive(query.Actor, ct);
        var userId = query.UserId ?? query.Actor.UserId;
        await RequireAccess(query.Actor, userId, ct);
        var year = query.Year ?? calendar.Year(calendar.Today);
        var allTypes = await types.GetActiveAsync(ct);
        var allocations = await balances.GetForYearAsync(userId, year, ct);
        var pending = await requests.GetPendingDaysAsync(userId, calendar.YearStart(year), calendar.YearStart(year + 1), ct);
        return allTypes.Select(type =>
        {
            var allocation = allocations.SingleOrDefault(b => b.LeaveTypeId == type.Id);
            return new LeaveBalanceDto(type.Id, type.Name, type.Code.ToString(), year,
                allocation?.AllocatedDays ?? 0, allocation?.UsedDays ?? 0,
                pending.GetValueOrDefault(type.Id), allocation?.RemainingDays ?? 0);
        }).ToList();
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> Handle(GetLeaveTypes query, CancellationToken ct)
    {
        await RequireActive(query.Actor, ct);
        return (await types.GetActiveAsync(ct)).Select(MapType).ToList();
    }

    public async Task<LeaveTypeDto> Handle(GetLeaveType query, CancellationToken ct)
    {
        await RequireActive(query.Actor, ct);
        var type = await types.GetAsync(query.Id, ct);
        return type is { IsActive: true, IsDeleted: false } ? MapType(type) : throw new KeyNotFoundException("Leave type not found.");
    }

    private async Task RequireActive(LeaveActor actor, CancellationToken ct)
    {
        if (actor.UserId <= 0 || await employees.GetAsync(actor.UserId, ct) is not { IsActive: true })
            throw new ResourceForbiddenException("An active employee account is required.");
    }

    private async Task RequireAccess(LeaveActor actor, long userId, CancellationToken ct)
    {
        if (actor.UserId == userId) return;
        var employee = await employees.GetAsync(userId, ct) ?? throw new KeyNotFoundException("Employee not found.");
        if (!actor.IsHRAdmin && !(actor.IsManager && employee.DepartmentManagerId == actor.UserId))
            throw new ResourceForbiddenException("You cannot access this employee's leave data.");
    }

    private async Task<LeaveRequest> OwnedRequest(long id, long userId)
    {
        var request = await requests.GetByIdAsync(id);
        return request is { IsDeleted: false } && request.UserId == userId ? request
            : throw new KeyNotFoundException("Leave request not found.");
    }

    private static LeaveTypeDto MapType(LeaveType type) => new(type.Id, type.Name, type.Code.ToString(), type.DefaultDaysPerYear);
    private static LeaveRequestDto Map(LeaveRequest request) => new(request.Id, request.UserId, request.LeaveTypeId,
        request.LeaveType.Name, request.StartDate, request.EndDate, request.NumberOfDays, request.IsHalfDay,
        request.HalfDayType?.ToString(), request.Reason, request.Status.ToString(), request.CreatedAt, request.RejectionComment);
}
