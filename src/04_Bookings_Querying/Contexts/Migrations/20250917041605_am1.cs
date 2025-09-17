using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _04_Bookings_Querying.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class am1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "bookings",
                table: "Guests",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "bookings",
                table: "Guests",
                columns: new[] { "Id", "Email", "IsDeleted", "IsVipYN", "Name", "UpdatedAt" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "alice@example.com", false, "Y", "Alice", null });
        }
    }
}
