namespace Levio.SharedKernel.Security;

public static class AuthorizationPolicies
{
    public const string RequireAuthenticated = nameof(RequireAuthenticated);
    public const string RequireEmployee = nameof(RequireEmployee);
    public const string RequireManager = nameof(RequireManager);
    public const string RequireHRAdmin = nameof(RequireHRAdmin);
}
