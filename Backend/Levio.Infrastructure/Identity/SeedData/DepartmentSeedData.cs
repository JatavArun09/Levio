using Levio.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Levio.Infrastructure.Identity.SeedData
{
    public class DepartmentSeedData : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasData(
                new Department
                {
                    Id = 1,
                    Name = "Administration",
                    Description = "System Administration",
                    CreatedAt = new DateTime(2026, 6, 1)
                });
        }
    }
}
