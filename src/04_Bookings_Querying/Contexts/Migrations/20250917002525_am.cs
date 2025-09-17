using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _04_Bookings_Querying.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class am : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price_Currency",
                schema: "bookings",
                table: "Reservations",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "Price_Amount",
                schema: "bookings",
                table: "Reservations",
                newName: "Amount");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "bookings",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                schema: "bookings",
                table: "Reservations",
                newName: "Price_Currency");

            migrationBuilder.RenameColumn(
                name: "Amount",
                schema: "bookings",
                table: "Reservations",
                newName: "Price_Amount");

            migrationBuilder.AlterColumn<string>(
                name: "Price_Currency",
                schema: "bookings",
                table: "Reservations",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
