namespace Levio.Modules.Identity.Application.Configurations;

public sealed class InitialAdministratorSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Administrator";
    public long DepartmentId { get; set; } = 1;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Password) ||
        !string.IsNullOrWhiteSpace(EmployeeCode);
}
