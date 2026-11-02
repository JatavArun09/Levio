using Levio.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static ArchitectureChecks;

internal static class DatabaseIntegrationChecks
{
    public static async Task RunAsync()
    {
        // An explicit test server is required. Never read the application's connection string.
        var server = Environment.GetEnvironmentVariable("LEVIO_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(server))
        {
            Console.WriteLine("SKIP: Fresh SQL Server migration test (set LEVIO_TEST_SQLSERVER to a disposable test server connection string).");
            return;
        }
        var connection = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = "Levio_ModularTests_" + Guid.NewGuid().ToString("N")
        };
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync();
            Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Pending migrations after fresh database creation.");
            Check(await db.Users.AnyAsync(u => u.Id == 1), "Seeded admin missing.");
            Check(await db.Roles.CountAsync() == 3, "Seeded roles missing.");
            Check(await db.LeaveTypes.CountAsync() == 4, "Seeded leave types missing.");
            Check((await db.Users.Include(u => u.UserRoles).ThenInclude(r => r.Role).SingleAsync(u => u.Id == 1))
                .UserRoles.Any(r => r.Role.Name == "HRAdmin"), "Seeded admin role association missing.");
            Console.WriteLine("PASS: Fresh disposable SQL Server database migrations and seed queries.");
            await LeaveWorkflowChecks.RunAsync(connection.ConnectionString, db);
        }
        finally
        {
            // The catalog is generated above and cannot reference the application database.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
