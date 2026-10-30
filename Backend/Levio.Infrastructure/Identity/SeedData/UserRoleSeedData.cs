using Levio.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Infrastructure.Identity.SeedData
{
    public class UserRoleSeedData :IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasData(
                new UserRole
                {
                    Id = 1,
                    UserId = 1, // System Admin
                    RoleId = 3  // HRAdmin
                }
            );
        }
    }
}
