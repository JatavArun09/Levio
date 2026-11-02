using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Levio.Modules.LeaveManagement.Application;

internal sealed class LeaveRequestInputValidator : AbstractValidator<LeaveRequestInput>
{
    public LeaveRequestInputValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).NotEqual(default(DateOnly)).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x).Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("A request cannot span more than one year.");
        RuleFor(x => x.Reason).NotEmpty().Must(x => x is not null && x.Trim().Length is >= 10 and <= 500)
            .WithMessage("Reason must contain 10 to 500 characters.")
            .Must(x => x is null || (!x.Contains('<') && !x.Contains('>'))).WithMessage("Reason must be plain text.");
        RuleFor(x => x.HalfDayType).NotNull().When(x => x.IsHalfDay);
        RuleFor(x => x.HalfDayType).IsInEnum().When(x => x.HalfDayType.HasValue);
        RuleFor(x => x.HalfDayType).Null().When(x => !x.IsHalfDay);
        RuleFor(x => x.EndDate).Equal(x => x.StartDate).When(x => x.IsHalfDay)
            .WithMessage("Half-day leave must start and end on the same day.");
    }
}

internal sealed class SaveLeaveRequestValidator : AbstractValidator<SaveLeaveRequest>
{
    public SaveLeaveRequestValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new LeaveRequestInputValidator());
        RuleFor(x => x.Id).GreaterThan(0).When(x => x.Id.HasValue);
    }
}

internal sealed class GetLeaveHistoryValidator : AbstractValidator<GetLeaveHistory>
{
    public GetLeaveHistoryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

internal sealed class GetLeaveBalancesValidator : AbstractValidator<GetLeaveBalances>
{
    public GetLeaveBalancesValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(1900, 9998).When(x => x.Year.HasValue);
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
    }
}

internal sealed class LeaveValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators,
    ILogger<LeaveValidationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        foreach (var validator in validators) await validator.ValidateAndThrowAsync(request, cancellationToken);
        logger.LogDebug("Handling {RequestType}", typeof(TRequest).Name);
        return await next();
    }
}
