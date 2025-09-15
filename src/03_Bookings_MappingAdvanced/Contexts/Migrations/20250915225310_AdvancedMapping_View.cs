using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _03_Bookings_MappingAdvanced.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedMapping_View : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            IF OBJECT_ID(N'[bookings].[vwReservationSummary]', N'V') IS NOT NULL
                DROP VIEW [bookings].[vwReservationSummary];
            ");
            
            migrationBuilder.Sql(@"
            CREATE VIEW [bookings].[vwReservationSummary] AS
            SELECT r.Number AS RoomNumber,
                COUNT(res.Id)          AS ReservationsCount,
                SUM(res.Price_Amount)  AS TotalAmount
            FROM (
                SELECT Id, RoomId, Price_Amount
                FROM   [bookings].[Reservations]
            ) res
            JOIN (
                SELECT Id, Number FROM [bookings].[StandardRooms]
                UNION ALL
                SELECT Id, Number FROM [bookings].[Suites]
            ) r ON r.Id = res.RoomId
            GROUP BY r.Number;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID(N'[bookings].[vwReservationSummary]', N'V') IS NOT NULL
                           DROP VIEW [bookings].[vwReservationSummary];");
        }
    }
}
