using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Levio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DisableDefaultAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L,
                column: "ISActive",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1L,
                column: "ISActive",
                value: true);
        }
    }
}
