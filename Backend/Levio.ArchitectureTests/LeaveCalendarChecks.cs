using Levio.Modules.LeaveManagement.Application;
using Levio.Modules.LeaveManagement.Domain.Enums;
using Microsoft.Extensions.Options;
using static ArchitectureChecks;

internal static class LeaveCalendarChecks
{
    public static void Run()
    {
        var calendar = new LeaveCalendar(Options.Create(new LeaveCalendarOptions { Holidays = [new(2026, 6, 17)] }),
            new FixedClock(new DateTimeOffset(2026, 6, 14, 20, 0, 0, TimeSpan.Zero)));
        Check(calendar.Today == new DateOnly(2026, 6, 15), "Organization time zone not respected.");
        Check(calendar.Year(new(2026, 3, 31)) == 2025 && calendar.Year(new(2026, 4, 1)) == 2026, "Financial year boundary incorrect.");
        Check(calendar.Count(new(2026, 6, 15), new(2026, 6, 21), LeaveTypeCode.CL, false) == 4, "Weekends/holiday counted as CL.");
        Check(calendar.Count(new(2026, 6, 15), new(2026, 6, 21), LeaveTypeCode.WFH, false) == 7, "WFH must count calendar days.");
        Check(calendar.Count(new(2026, 6, 15), new(2026, 6, 15), LeaveTypeCode.SL, true) == .5m, "Half-day duration incorrect.");
        Check(calendar.Count(new(2026, 6, 20), new(2026, 6, 21), LeaveTypeCode.CL, false) == 0, "Weekend-only duration incorrect.");
        Console.WriteLine("PASS: Leave calendar, financial year, holidays, WFH and half-day calculations.");
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
