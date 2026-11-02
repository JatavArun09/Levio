using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Levio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAdminSeed1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "Description", "IsDeleted", "ManagerId", "Name", "UpdatedAt" },
                values: new object[] { 1L, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "System Administration", false, null, "Administration", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DateOfJoining", "DepartmentId", "Email", "EmployeeCode", "FirstName", "ISActive", "IsDeleted", "LastName", "PasswordHash", "UpdatedAt" },
                values: new object[] { 1L, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L, "admin@levio.com", "EMP001", "System", true, false, "Admin", "AQAAAAIAAYagAAAAEJ1EaeE3EgsZvxXpcO8j45nWHbhcZ9FWWcsB/NNqDvSj48zvRYl/60BVWFWrD4KrKA==", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "CreatedAt", "IsDeleted", "RoleId", "UpdatedAt", "UserId" },
                values: new object[] { 1L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, 3L, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1L);
        }
    }
}
