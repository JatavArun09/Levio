using System.Security.Claims;
using Levio.Modules.LeaveManagement.Application;
using Levio.Modules.LeaveManagement.Domain.Enums;
using Levio.SharedKernel.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Levio.Modules.LeaveManagement.Presentation;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireEmployee)]
public abstract class LeaveControllerBase : ControllerBase
{
    protected LeaveActor Actor => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) && id > 0
        ? new LeaveActor(id, User.IsInRole("Manager"), User.IsInRole("HRAdmin"))
        : throw new UnauthorizedAccessException("A valid employee identity is required.");
}

[Route("api/v1/leave-requests")]
public sealed class LeaveRequestsController(ISender sender) : LeaveControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10,
        [FromQuery] LeaveStatus? status = null, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetLeaveHistory(Actor, pageNumber, pageSize, status), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LeaveRequestDto>>.Ok(result.Items, meta: result.Meta));
    }

    [HttpGet("{id:long:min(1)}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
        => Ok(ApiResponse<LeaveRequestDto>.Ok(await sender.Send(new GetLeaveRequest(Actor, id), ct)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeaveRequestInput input, CancellationToken ct)
    {
        var result = await sender.Send(new SaveLeaveRequest(Actor, null, input), ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, ApiResponse<LeaveRequestDto>.Ok(result, "Leave request created."));
    }

    [HttpPut("{id:long:min(1)}")]
    public async Task<IActionResult> Update(long id, [FromBody] LeaveRequestInput input, CancellationToken ct)
        => Ok(ApiResponse<LeaveRequestDto>.Ok(await sender.Send(new SaveLeaveRequest(Actor, id, input), ct), "Draft updated."));

    [HttpDelete("{id:long:min(1)}")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await sender.Send(new CancelLeaveRequest(Actor, id), ct);
        return NoContent();
    }
}

[Route("api/v1/leave-balances")]
public sealed class LeaveBalancesController(ISender sender) : LeaveControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Own([FromQuery] int? year, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(await sender.Send(new GetLeaveBalances(Actor, Year: year), ct)));

    [HttpGet("{userId:long:min(1)}")]
    [Authorize(Policy = AuthorizationPolicies.RequireManager)]
    public async Task<IActionResult> Employee(long userId, [FromQuery] int? year, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<LeaveBalanceDto>>.Ok(await sender.Send(new GetLeaveBalances(Actor, userId, year), ct)));
}

[Route("api/v1/leave-types")]
public sealed class LeaveTypesController(ISender sender) : LeaveControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<LeaveTypeDto>>.Ok(await sender.Send(new GetLeaveTypes(Actor), ct)));

    [HttpGet("{id:long:min(1)}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
        => Ok(ApiResponse<LeaveTypeDto>.Ok(await sender.Send(new GetLeaveType(Actor, id), ct)));
}
