using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using FluentValidation;
using MediatR;
using Levio.Modules.LeaveManagement.Application;
using Levio.Modules.LeaveManagement.Presentation;

namespace Levio.Modules.LeaveManagement;

public static class LeaveManagementModule
{
    public static IServiceCollection AddLeaveManagementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LeaveCalendarOptions>().Bind(configuration.GetSection("LeaveCalendar"))
            .Validate(o => o.YearStartMonth is >= 1 and <= 12, "Leave year start month must be between 1 and 12.")
            .Validate(o => o.MaxRequestDays is > 0 and <= 366 && o.MaxCasualLeaveDays > 0 && o.EarnedLeaveNoticeDays >= 0,
                "Leave duration and notice settings are invalid.")
            .Validate(o => ValidTimeZone(o.TimeZoneId), "Leave time zone is invalid.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LeaveCalendar>();
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssembly(typeof(LeaveManagementModule).Assembly);
            options.AddOpenBehavior(typeof(LeaveValidationBehavior<,>));
        });
        services.AddTransient<IValidator<SaveLeaveRequest>, SaveLeaveRequestValidator>();
        services.AddTransient<IValidator<GetLeaveHistory>, GetLeaveHistoryValidator>();
        services.AddTransient<IValidator<GetLeaveBalances>, GetLeaveBalancesValidator>();
        services.AddControllers().AddApplicationPart(typeof(LeaveRequestsController).Assembly);
        return services;
    }

    private static bool ValidTimeZone(string timeZoneId)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); return true; }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException) { return false; }
    }
}
