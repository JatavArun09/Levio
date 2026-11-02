using System.Text.Json.Serialization;
using Levio.Modules.LeaveManagement.Domain.Enums;

namespace Levio.Modules.LeaveManagement.Application;

public sealed record LeaveActor(long UserId, bool IsManager, bool IsHRAdmin);

public sealed record LeaveRequestInput
{
    public long LeaveTypeId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public bool IsHalfDay { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter<HalfDayType>))]
    public HalfDayType? HalfDayType { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool SaveAsDraft { get; init; }
}

public sealed record LeaveRequestDto(long Id, long UserId, long LeaveTypeId, string LeaveType,
    DateOnly StartDate, DateOnly EndDate, decimal NumberOfDays, bool IsHalfDay, string? HalfDayType,
    string Reason, string Status, DateTime AppliedAt, string? RejectionComment);
public sealed record LeaveTypeDto(long Id, string Name, string Code, int DefaultDaysPerYear);
public sealed record LeaveBalanceDto(long LeaveTypeId, string LeaveType, string Code, int Year,
    decimal AllocatedDays, decimal UsedDays, decimal PendingDays, decimal AvailableDays);
public sealed record PageMeta(int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
public sealed record Page<T>(IReadOnlyList<T> Items, PageMeta Meta);

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, string[] Errors, PageMeta? Meta = null)
{
    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.", PageMeta? meta = null)
        => new(true, message, data, [], meta);
}
