using Levio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Infrastructure.SeedData
{
    public class RoleSeedData: IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasData(
            new Role
            {
                Id = 1,
                Name = "Employee",
                Description = "Employee role",
                CreatedAt = new DateTime(2026, 6, 1)
            },
            new Role
            {
                Id = 2,
                Name = "Manager",
                Description = "Manager role",
                CreatedAt = new DateTime(2026, 6, 1)
            },
            new Role
            {
                Id = 3,
                Name = "HRAdmin",
                Description = "HR Administrator",
                CreatedAt = new DateTime(2026, 6, 1)
            });
        }
    }
}
