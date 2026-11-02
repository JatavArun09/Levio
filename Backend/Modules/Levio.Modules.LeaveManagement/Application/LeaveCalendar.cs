using Levio.Modules.LeaveManagement.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Levio.Modules.LeaveManagement.Application;

public sealed class LeaveCalendarOptions
{
    public int YearStartMonth { get; set; } = 4;
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public DateOnly[] Holidays { get; set; } = [];
    public int MaxRequestDays { get; set; } = 30;
    public int MaxCasualLeaveDays { get; set; } = 5;
    public int EarnedLeaveNoticeDays { get; set; } = 7;
}

public sealed class LeaveCalendar(IOptions<LeaveCalendarOptions> settings, TimeProvider timeProvider)
{
    private readonly LeaveCalendarOptions options = settings.Value;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(),
        TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId)).DateTime);
    public int Year(DateOnly date) => date.Month >= options.YearStartMonth ? date.Year : date.Year - 1;
    public DateOnly YearStart(int year) => new(year, options.YearStartMonth, 1);
    public decimal Count(DateOnly start, DateOnly end, LeaveTypeCode type, bool halfDay)
    {
        decimal days = 0;
        for (var number = start.DayNumber; number <= end.DayNumber; number++)
        {
            var date = DateOnly.FromDayNumber(number);
            if (type == LeaveTypeCode.WFH || (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday
                && !options.Holidays.Contains(date))) days++;
        }
        return halfDay ? days / 2 : days;
    }
}
