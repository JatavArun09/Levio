using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Levio.Infrastructure.Data;
using Levio.Modules.Identity.Application.Interfaces;
using Levio.Modules.Identity.Domain.Entities;
using Levio.Modules.LeaveManagement.Application;
using Levio.Modules.LeaveManagement.Domain.Entities;
using Levio.SharedKernel.Persistence;
using Levio.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using static ArchitectureChecks;
using DomainHalfDay = Levio.Modules.LeaveManagement.Domain.Enums.HalfDayType;

internal static class LeaveWorkflowChecks
{
    public static async Task RunAsync(string connection, ApplicationDbContext db)
    {
        var department = new Department { Name = "Other team" };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        var manager = Employee("manager", 1, 2);
        var employee = Employee("employee", 1, 1);
        var peer = Employee("peer", 1, 1);
        var otherManager = Employee("other-manager", department.Id, 2);
        var inactive = Employee("inactive", 1, 1);
        inactive.ISActive = false;
        db.Users.AddRange(manager, employee, peer, otherManager, inactive);
        await db.SaveChangesAsync();
        (await db.Departments.FindAsync(1L))!.ManagerId = manager.Id;
        department.ManagerId = otherManager.Id;
        db.LeaveBalances.AddRange(new[] { (1L, 12m), (2L, 12m), (3L, 21m), (4L, 24m) }.Select(item => new LeaveBalance
        {
            UserId = employee.Id, LeaveTypeId = item.Item1, Year = 2026, AllocatedDays = item.Item2, RemainingDays = item.Item2
        }));
        await db.SaveChangesAsync();
        var failures = new FailureSwitch();
        await db.Roles.LoadAsync();
        await using var app = ApiHost.Build(["--environment", "Development"], builder =>
        {
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["JwtSettings:Issuer"] = "Levio.LeaveTests", ["JwtSettings:Audience"] = "Levio.LeaveTests",
                ["JwtSettings:SecretKey"] = "Levio-leave-workflow-tests-signing-key-1234567890",
                ["JwtSettings:AccessTokenMinutes"] = "15", ["JwtSettings:RefreshTokenDays"] = "7",
                ["HttpsRedirection:Enabled"] = "false", ["LeaveCalendar:Holidays:0"] = "2026-06-17"
            });
            builder.Services.RemoveAll<TimeProvider>();
            builder.Services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 6, 15, 6, 0, 0, TimeSpan.Zero)));
            builder.Services.RemoveAll<IUnitOfWork>();
            builder.Services.AddScoped<IUnitOfWork>(sp => new TestUnitOfWork(sp.GetRequiredService<ApplicationDbContext>(), failures));
        });
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(20) };
        await Expect(client.GetAsync("/api/v1/leave-balances"), HttpStatusCode.Unauthorized);
        string Token(User user)
        {
            using var scope = app.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IJwtTokenService>().GenerateAccessToken(user);
        }
        void ActAs(User user) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(user));
        ActAs(employee);
        var types = await Read<IReadOnlyList<LeaveTypeDto>>(client.GetAsync("/api/v1/leave-types"));
        Check(types.Count == 4, "Active leave types missing.");
        await Expect(client.GetAsync("/api/v1/leave-types/99999"), HttpStatusCode.NotFound);
        var initial = await OwnBalances();
        Check(initial.All(b => b.Year == 2026) && initial.Single(b => b.Code == "CL").AvailableDays == 12, "Initial financial-year balances incorrect.");
        var created = await Submit(Input(1, 15, 19));
        Check(created.Status == "Pending" && created.UserId == employee.Id && created.NumberOfDays == 4, "Submission duration/status/owner incorrect.");
        var after = (await OwnBalances()).Single(b => b.Code == "CL");
        Check(after.AvailableDays == 8 && after.PendingDays == 4 && after.UsedDays == 0, "Pending leave was not reserved separately.");
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(2, 16, 16)), HttpStatusCode.Conflict);
        Check(await Remaining(2) == 12, "Overlap failure deducted balance.");
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.NoContent);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.Conflict);
        Check(await Remaining(1) == 12, "Cancellation must restore balance exactly once.");

        var draft = await Submit(Input(2, 18, 18) with { SaveAsDraft = true });
        Check(draft.Status == "Draft" && await Remaining(2) == 12, "Draft reserved balance.");
        var submitted = await Read<LeaveRequestDto>(client.PutAsJsonAsync($"/api/v1/leave-requests/{draft.Id}", Input(1, 22, 22)));
        Check(submitted.Status == "Pending" && submitted.LeaveTypeId == 1 && await Remaining(1) == 11, "Draft submission failed.");
        await Expect(client.PutAsJsonAsync($"/api/v1/leave-requests/{draft.Id}", Input(1, 22, 22)), HttpStatusCode.Conflict);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{draft.Id}"), HttpStatusCode.NoContent);
        var half = Input(1, 23, 23) with { IsHalfDay = true, HalfDayType = DomainHalfDay.AM };
        var am = await Submit(half);
        var pm = await Submit(half with { HalfDayType = DomainHalfDay.PM });
        Check(am.NumberOfDays == .5m && pm.NumberOfDays == .5m && await Remaining(1) == 11, "AM/PM allocation incorrect.");
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", half), HttpStatusCode.Conflict);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(2, 23, 23)), HttpStatusCode.Conflict);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{am.Id}"), HttpStatusCode.NoContent);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{pm.Id}"), HttpStatusCode.NoContent);
        Check(await Remaining(1) == 12, "Half-day cancellation failed.");

        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 15, 15) with { Reason = "short" }), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 15, 15) with { Reason = new string('x', 501) }), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 15, 15) with { Reason = "<script>unsafe</script>" }), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 14, 14)), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 19, 15)), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", half with { HalfDayType = null }), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", half with { HalfDayType = (DomainHalfDay)3 }), HttpStatusCode.BadRequest);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", half with { LeaveTypeId = 3 }), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 20, 21)), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 22, 29)), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(3, 16, 16)), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 15, 15) with
        { StartDate = new(2027, 3, 31), EndDate = new(2027, 4, 1) }), HttpStatusCode.UnprocessableEntity);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(3, 15, 15) with
        { StartDate = new(2026, 7, 1), EndDate = new(2026, 7, 31) }), HttpStatusCode.UnprocessableEntity);
        await Expect(client.GetAsync("/api/v1/leave-requests?pageSize=101"), HttpStatusCode.BadRequest);
        await Expect(client.GetAsync("/api/v1/leave-requests?status=999"), HttpStatusCode.BadRequest);
        await Expect(client.GetAsync("/api/v1/leave-balances?year=10000"), HttpStatusCode.BadRequest);
        var wfh = await Submit(Input(4, 20, 21));
        Check(wfh.NumberOfDays == 2 && await Remaining(4) == 22 && await Remaining(1) == 12, "WFH must use its own calendar-day balance.");
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{wfh.Id}"), HttpStatusCode.NoContent);

        ActAs(peer);
        await Expect(client.GetAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.Forbidden);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.NotFound);
        await Expect(client.GetAsync($"/api/v1/leave-balances/{employee.Id}"), HttpStatusCode.Forbidden);
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 15, 15)), HttpStatusCode.UnprocessableEntity);
        Check((await OwnBalances()).All(b => b.AvailableDays == 0), "Unallocated employee must have zero available days.");
        ActAs(manager);
        await Expect(client.GetAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.OK);
        await Expect(client.GetAsync($"/api/v1/leave-balances/{employee.Id}"), HttpStatusCode.OK);
        await Expect(client.DeleteAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.NotFound);
        ActAs(otherManager);
        await Expect(client.GetAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.Forbidden);
        await Expect(client.GetAsync($"/api/v1/leave-balances/{employee.Id}"), HttpStatusCode.Forbidden);
        ActAs((await db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role).SingleAsync(u => u.Id == 1)));
        await Expect(client.GetAsync($"/api/v1/leave-requests/{created.Id}"), HttpStatusCode.OK);
        ActAs(inactive);
        await Expect(client.GetAsync("/api/v1/leave-balances"), HttpStatusCode.Forbidden);
        ActAs(employee);
        using (var historyResponse = await client.GetAsync("/api/v1/leave-requests?pageNumber=1&pageSize=2&status=Cancelled"))
        {
            var history = (await historyResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<LeaveRequestDto>>>())!;
            Check(history.Success && history.Data!.Count == 2 && history.Meta!.TotalCount >= 5 && history.Meta.TotalPages >= 3,
                "History pagination/status filtering incorrect.");
        }
        var firstPage = await Read<IReadOnlyList<LeaveRequestDto>>(client.GetAsync("/api/v1/leave-requests?pageSize=1"));
        var secondPage = await Read<IReadOnlyList<LeaveRequestDto>>(client.GetAsync("/api/v1/leave-requests?pageSize=1&pageNumber=2"));
        Check(firstPage[0].Id != secondPage[0].Id, "History pagination is unstable.");

        db.ChangeTracker.Clear();
        var cl = await db.LeaveBalances.SingleAsync(b => b.UserId == employee.Id && b.LeaveTypeId == 1);
        cl.AllocatedDays = cl.RemainingDays = 1;
        await db.SaveChangesAsync();
        var concurrent = await Task.WhenAll(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 24, 24)),
            client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 25, 25)));
        Check(concurrent.Count(r => r.StatusCode == HttpStatusCode.Created) == 1
            && concurrent.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity) == 1 && await Remaining(1) == 0,
            "Concurrent applications overspent the balance.");
        foreach (var response in concurrent) response.Dispose();
        await db.Entry(cl).ReloadAsync();
        cl.AllocatedDays = 3; cl.RemainingDays = 2;
        await db.SaveChangesAsync();
        concurrent = await Task.WhenAll(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 26, 26)),
            client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 26, 26)));
        Check(concurrent.Count(r => r.StatusCode == HttpStatusCode.Created) == 1
            && concurrent.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1 && await Remaining(1) == 1,
            "Concurrent applications bypassed overlap checks.");
        foreach (var response in concurrent) response.Dispose();
        var countBeforeFailure = await db.LeaveRequests.CountAsync();
        failures.FailNext = true;
        await Expect(client.PostAsJsonAsync("/api/v1/leave-requests", Input(1, 29, 29)), HttpStatusCode.InternalServerError);
        Check(await Remaining(1) == 1 && await db.LeaveRequests.CountAsync() == countBeforeFailure,
            "Failed save did not roll back both request and balance.");
        await app.StopAsync();
        Console.WriteLine("PASS: SQL-backed leave HTTP workflows, validation, ownership/team access, financial-year balances, concurrency and rollback.");

        async Task<IReadOnlyList<LeaveBalanceDto>> OwnBalances() => await Read<IReadOnlyList<LeaveBalanceDto>>(client.GetAsync("/api/v1/leave-balances"));
        async Task<decimal> Remaining(long type) => (await db.LeaveBalances.AsNoTracking()
            .SingleAsync(b => b.UserId == employee.Id && b.LeaveTypeId == type)).RemainingDays;
        async Task<LeaveRequestDto> Submit(LeaveRequestInput input) => await Read<LeaveRequestDto>(
            client.PostAsJsonAsync("/api/v1/leave-requests", input), HttpStatusCode.Created);
    }

    private static User Employee(string name, long departmentId, long roleId) => new()
    {
        EmployeeCode = "TEST-" + name, FirstName = "Test", LastName = name, Email = name + "@leave-tests.invalid",
        DepartmentId = departmentId, UserRoles = [new UserRole { RoleId = roleId }]
    };
    private static LeaveRequestInput Input(long type, int start, int end) => new()
    {
        LeaveTypeId = type, StartDate = new(2026, 6, start), EndDate = new(2026, 6, end), Reason = "Personal family arrangements."
    };
    private static async Task<T> Read<T>(Task<HttpResponseMessage> request, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var response = await request;
        Check(response.StatusCode == expected, $"{response.RequestMessage!.RequestUri}: expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var envelope = (await response.Content.ReadFromJsonAsync<ApiResponse<T>>())!;
        Check(envelope.Success && envelope.Data is not null, "Success response envelope missing.");
        return envelope.Data!;
    }
    private static async Task Expect(Task<HttpResponseMessage> request, HttpStatusCode status)
    {
        using var response = await request;
        Check(response.StatusCode == status, $"{response.RequestMessage!.RequestUri}: expected {status}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        if ((int)status >= 400)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check(!json.RootElement.GetProperty("success").GetBoolean() && json.RootElement.TryGetProperty("errors", out _),
                "Versioned API error envelope missing.");
        }
    }

    private sealed class FailureSwitch { public bool FailNext; }
    private sealed class TestUnitOfWork(ApplicationDbContext db, FailureSwitch failures) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var count = await db.SaveChangesAsync(ct);
            if (failures.FailNext) { failures.FailNext = false; throw new InvalidOperationException("Simulated failure after database save."); }
            return count;
        }
    }
}
