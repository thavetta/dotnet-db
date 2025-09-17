using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _03_Bookings_MappingAdvanced.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedMapping_Start : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bookings");

            migrationBuilder.CreateSequence(
                name: "RoomSequence",
                schema: "bookings");

            migrationBuilder.CreateTable(
                name: "Guests",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsVipYN = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardRooms",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [bookings].[RoomSequence]"),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 2)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suites",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR [bookings].[RoomSequence]"),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    HasLounge = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Price_Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Guests_GuestId",
                        column: x => x.GuestId,
                        principalSchema: "bookings",
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "bookings",
                table: "Guests",
                columns: new[] { "Id", "Email", "IsDeleted", "IsVipYN", "Name", "UpdatedAt" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "alice@example.com", false, "Y", "Alice", null });

            migrationBuilder.InsertData(
                schema: "bookings",
                table: "Settings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { 1, "RetentionDays", "90" },
                    { 2, "Theme", "dark" }
                });

            migrationBuilder.InsertData(
                schema: "bookings",
                table: "StandardRooms",
                columns: new[] { "Id", "Capacity", "Number" },
                values: new object[] { 1, 2, "101" });

            migrationBuilder.InsertData(
                schema: "bookings",
                table: "Suites",
                columns: new[] { "Id", "Capacity", "HasLounge", "Number" },
                values: new object[] { 2, 2, true, "101" });

            migrationBuilder.InsertData(
                schema: "bookings",
                table: "Reservations",
                columns: new[] { "Id", "Price_Amount", "Price_Currency", "CheckIn", "CheckOut", "GuestId", "RoomId", "Status" },
                values: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 120m, "EUR", new DateTime(2025, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 1, "Confirmed" });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Name",
                schema: "bookings",
                table: "Guests",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_GuestId",
                schema: "bookings",
                table: "Reservations",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId_CheckIn_CheckOut",
                schema: "bookings",
                table: "Reservations",
                columns: new[] { "RoomId", "CheckIn", "CheckOut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "bookings");

            migrationBuilder.DropTable(
                name: "Settings",
                schema: "bookings");

            migrationBuilder.DropTable(
                name: "StandardRooms",
                schema: "bookings");

            migrationBuilder.DropTable(
                name: "Suites",
                schema: "bookings");

            migrationBuilder.DropTable(
                name: "Guests",
                schema: "bookings");

            migrationBuilder.DropSequence(
                name: "RoomSequence",
                schema: "bookings");
        }
    }
}
