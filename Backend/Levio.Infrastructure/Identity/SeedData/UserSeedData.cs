using Levio.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Levio.Infrastructure.Identity.SeedData
{
    public class UserSeedData:IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasData(
                new User
                {
                    Id = 1,
                    EmployeeCode = "EMP001",
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@levio.com",
                    PasswordHash = "AQAAAAIAAYagAAAAEJ1EaeE3EgsZvxXpcO8j45nWHbhcZ9FWWcsB/NNqDvSj48zvRYl/60BVWFWrD4KrKA==",
                    DepartmentId = 1,
                    // This account remains disabled. Provision the first HR administrator out-of-band
                    // with a unique password stored outside source control.
                    ISActive = false,
                    CreatedAt = new DateTime(2026, 6, 1)
                }
            );
        }
    }
}
