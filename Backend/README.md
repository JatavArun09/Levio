# Levio backend

Levio runs as one ASP.NET Core (.NET 10) application. Business code is organized
into independent module projects; all modules currently use one SQL Server
database through a shared EF Core context.

## Module ownership

| Project | Owns |
| --- | --- |
| `Modules/Levio.Modules.Identity` | Users, departments, roles, authentication endpoints, password hashing and JWT services |
| `Modules/Levio.Modules.LeaveManagement` | Leave types, calendar rules, requests, balances, validators, CQRS handlers and leave API endpoints |
| `Modules/Levio.Modules.Identity.Contracts` | Employee directory DTO and interface used across module boundaries |
| `Modules/Levio.Modules.Auditing` | Audit record domain model |
| `Levio.SharedKernel` | Base entity, base domain exception, generic repository and unit-of-work abstractions |
| `Levio.Infrastructure` | Shared SQL Server context, module-grouped EF mappings, seeds, repository adapters and migrations |
| `Levio.WebApi` | Composition root, middleware, Swagger, hosting and diagnostic endpoints |

Identity has working authentication services and controllers. Leave Management
now implements the employee self-service milestone from roadmap phase 8, with
the repository, validation and authorization work needed from phases 4, 5 and 7.
Auditing still contains the domain foundation for its later roadmap phase.

## Dependency rules

```mermaid
flowchart TD
    Host[WebApi] --> Identity
    Host --> Leave[Leave Management]
    Host --> Audit[Auditing]
    Host --> Infra[Infrastructure]
    Infra --> Identity
    Infra --> Leave
    Infra --> Audit
    Identity --> Shared[SharedKernel]
    Leave --> Shared
    Identity --> Contracts[Identity Contracts]
    Leave --> Contracts
    Audit --> Shared
    Infra --> Shared
```

- Modules reference SharedKernel and explicitly permitted contract assemblies;
  they do not reference another module's implementation, Infrastructure, or EF
  Core. Domain and application folders live inside their owning module.
- Infrastructure implements repository ports declared by the owning module and
  registers persistence only. Each module registers its own application services.
- Cross-module relationships use scalar IDs. Infrastructure configures the
  existing foreign keys without entity navigation properties across modules.
- Entities and repository ports remain public for persistence adapters.
  Authentication service implementations are internal; the host uses module
  registration methods. Do not use public entity types as a shortcut for
  cross-module business calls.
- Leave Management uses `IEmployeeDirectory` from Identity.Contracts to check
  active employee status and the department's designated manager. Identity
  implements that interface. The contract project has no entity, repository,
  framework, or EF dependencies; architecture checks explicitly allow only these
  contract references.
- Shared persistence is an intentional compromise. There is one DbContext,
  transaction boundary, and migration history, so database changes remain
  coordinated. Module-specific contexts and independently deployed services are
  outside this conversion.

## Run locally

From the repository root:

```powershell
dotnet build Backend/Levio.slnx --configuration Release
dotnet run --project Backend/Levio.WebApi --launch-profile https
```

Install the .NET 10 SDK and configure `ConnectionStrings:DefaultConnection` for
SQL Server using the normal ASP.NET Core configuration providers. Configure
`JwtSettings:Issuer`, `Audience`, `AccessTokenMinutes`, and `RefreshTokenDays`;
provide `JwtSettings__SecretKey` only through a secret manager or environment
variable. Startup rejects missing issuer/audience, signing keys shorter than 32
UTF-8 bytes, and nonpositive token lifetimes. For a local PowerShell session:

```powershell
$env:JwtSettings__SecretKey = '<a unique secret of at least 32 UTF-8 bytes>'
```

The HTTPS launch profile listens on `https://localhost:7017`; Swagger is
available at `/swagger` in Development. HTTP is redirected to HTTPS and HSTS is
enabled outside Development.
Use Release configuration if an existing Debug API process locks its output files.

## Authentication and user flow

The login, refresh, and logout routes are anonymous; the current-user route
requires a bearer token:

- `POST /api/Auth/login`
- `POST /api/Auth/refresh-token`
- `POST /api/Auth/logout`
- `GET /api/Auth/me` (requires a bearer token)

All other endpoints require a valid bearer token by default. The former test and
diagnostic routes have been removed. Controller discovery explicitly includes
Identity.

Access tokens expire after 15 minutes by default. Refresh tokens are generated
cryptographically, stored only as SHA-256 hashes, rotated on use, and placed in a
`Secure`, `HttpOnly`, `SameSite=Strict` `__Host-levio-refresh` cookie. They are
never returned in JSON. Login and refresh are rate limited to five requests per
minute for each client IP. Token validation checks that the user is still active,
so disabling or deleting a user invalidates their authenticated requests.

There is no public registration route. The legacy seeded HR administrator is
disabled by migration `20261102050545_DisableDefaultAdministrator`; provision a
real active HR administrator through the one-time bootstrap configuration before
enabling user access:

```powershell
$env:InitialAdministrator__Email = 'hr.admin@example.com'
$env:InitialAdministrator__EmployeeCode = 'HR001'
$env:InitialAdministrator__Password = '<14+ character unique password with upper, lower, digit and symbol>'
```

The bootstrap only creates an account when no active HR administrator exists; it
never resets or reactivates an existing account. Store the password outside source
control and remove the bootstrap variables after the account has been created.

## Database and migrations

The API does not automatically apply migrations at startup. Apply the latest
migration before deploying this version; it disables the legacy default admin
account in existing databases.

From the repository root, use these commands when intentionally managing a
development database:

```powershell
dotnet ef migrations list --project Backend/Levio.Infrastructure --startup-project Backend/Levio.WebApi --configuration Release
dotnet ef migrations add YourChange --project Backend/Levio.Infrastructure --startup-project Backend/Levio.WebApi --configuration Release
dotnet ef database update --project Backend/Levio.Infrastructure --startup-project Backend/Levio.WebApi --configuration Release
```

New mappings and seeds go in the owning module's Infrastructure subfolder and
are discovered by `ApplicationDbContext`. Do not reset migration history or
create a replacement initial migration for existing databases.

## Leave self-service APIs

All new endpoints use `/api/v1`, require a bearer token, and return the documented
`success`, `message`, `data`, `errors` envelope. History includes pagination in
`meta`; successful cancellation returns HTTP 204. The legacy authentication
routes and their response shapes are unchanged.

| Method and route | Behavior |
| --- | --- |
| `GET /api/v1/leave-types` | List active types |
| `GET /api/v1/leave-types/{id}` | View active type details |
| `GET /api/v1/leave-balances?year=2026` | Own allocations, used, pending and available days |
| `GET /api/v1/leave-balances/{userId}?year=2026` | Designated department manager or HRAdmin access |
| `GET /api/v1/leave-requests?pageNumber=1&pageSize=10&status=Pending` | Own history, newest first; page size 1–100 |
| `GET /api/v1/leave-requests/{id}` | Owner, designated department manager, or HRAdmin |
| `POST /api/v1/leave-requests` | Submit Pending request, or save Draft |
| `PUT /api/v1/leave-requests/{id}` | Owner updates a Draft or submits it |
| `DELETE /api/v1/leave-requests/{id}` | Owner cancels Pending/Draft; restores a Pending reservation |

Employee, Manager and HRAdmin roles can use self-service. `RequireAuthenticated`,
`RequireEmployee`, `RequireManager` and `RequireHRAdmin` policies are registered.
The service independently checks active employee status and resource access
through the Identity contract. Request ownership always comes from the JWT,
never from a submitted employee ID.

Submit a request, choosing future/current dates:

```json
{
  "leaveTypeId": 1,
  "startDate": "2026-11-02",
  "endDate": "2026-11-03",
  "isHalfDay": false,
  "halfDayType": null,
  "reason": "Family function and travel arrangements.",
  "saveAsDraft": false
}
```

Set `saveAsDraft` to `true` to save without reserving balance. A PUT with
`saveAsDraft=false` submits an existing Draft and performs submission checks.
For half-day CL/SL, set the same start/end date, `isHalfDay=true`, and
`halfDayType="AM"` or `"PM"`. Corrected half-day and user-role enum names match
the specification; no database column or seeded value changes are required.

Validation rejects past dates, invalid/inactive leave types, reason text outside
10–500 characters or containing markup delimiters, invalid half-day combinations,
overlaps with Pending/Approved requests, insufficient balance and excessive
duration. Opposite AM/PM slots on the same working day may coexist. The default
limits are 30 eligible days per request, 5 consecutive CL days and 7 calendar
days' advance notice for EL. Defaults can be configured before deployment:

```json
{
  "LeaveCalendar": {
    "YearStartMonth": 4,
    "TimeZoneId": "Asia/Kolkata",
    "Holidays": [],
    "MaxRequestDays": 30,
    "MaxCasualLeaveDays": 5,
    "EarnedLeaveNoticeDays": 7
  }
}
```

The organization time zone defines today. CL/SL/EL exclude Saturdays, Sundays,
and configured holiday dates (`YYYY-MM-DD`). WFH includes calendar days and uses
its own allocation, without spending CL/SL/EL. The leave year is identified by
its starting year: `2026` means April 1, 2026 through March 31, 2027 by default.
Configure the real holiday list before use; none was supplied, so it is empty.
Keep the year-start setting stable once balances and requests exist.

Pending submission reduces `RemainingDays`; `UsedDays` remains the approved-use
counter for the approval phase. Drafts reserve nothing. Cancellation restores
the stored request duration exactly once. A database transaction and a
transaction-owned, per-employee SQL Server application lock serialize submission
and cancellation across API instances. Balance/request changes roll back together.

HR must initialize `LeaveBalances` for the employee, type and leave year before
submission; missing allocations are shown as zero and submission returns HTTP
422. Existing leave type seeds are preserved (including EL 18 and WFH 0 defaults,
which differ from the document's policy examples). Type defaults are descriptive,
not automatic grants. HR allocation and policy administration APIs are later work.

Malformed input returns 400, permission failures 401/403, missing records 404,
overlap/state/lock conflicts 409, and business rule/balance failures 422. New API
errors use the versioned envelope without exposing internal exception details.

### Scope decisions and next roadmap work

The document contradicts itself on backdated requests and cancellation of approved
leave. This milestone follows its explicit validation and API restrictions: no
backdated submission, and cancellation only for Pending/Draft. Approved cancellation,
manager re-acknowledgment and backdated exceptions await a confirmed workflow.
Cross-year requests must currently be split into separate applications so each
reservation has one unambiguous annual balance.

Next: phase 9 approval/rejection, team calendar and dashboard; then phase 10 audit
logging and phase 11 reports. Employee/department/policy administration, balance
allocation UI, password reset and lockout workflows, notifications and the React
frontend remain unfinished. No email delivery, document upload or production
deployment is included in this milestone.
