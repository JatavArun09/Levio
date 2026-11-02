using Levio.Domain.Entities;
using Levio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Levio.Infrastructure.SeedData
{
    public class LeaveTypeSeedData:IEntityTypeConfiguration<LeaveType>
    {
        public void Configure(EntityTypeBuilder<LeaveType> builder)
        {
            builder.HasData(
            new LeaveType
            {
                Id = 1,
                Name = "Casual Leave",
                Code = LeaveTypeCode.CL,
                DefaultDaysPerYear = 12,
                IsActive = true,
                CreatedAt = new DateTime(2026, 6, 1)
            },
            new LeaveType
            {
                Id = 2,
                Name = "Sick Leave",
                Code = LeaveTypeCode.SL,
                DefaultDaysPerYear = 12,
                IsActive = true,
                CreatedAt = new DateTime(2026, 6, 1)
            },
            new LeaveType
            {
                Id = 3,
                Name = "Earned Leave",
                Code = LeaveTypeCode.EL,
                DefaultDaysPerYear = 18,
                IsActive = true,
                CreatedAt = new DateTime(2026, 6, 1)
            },
            new LeaveType
            {
                Id = 4,
                Name = "Work From Home",
                Code = LeaveTypeCode.WFH,
                DefaultDaysPerYear = 0,
                IsActive = true,
                CreatedAt = new DateTime(2026, 6, 1)
            });
        }
    }
}
