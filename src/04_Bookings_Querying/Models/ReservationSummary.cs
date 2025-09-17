using Microsoft.EntityFrameworkCore;

namespace Bookings.Models;

// (g) Keyless entita navázaná na VIEW
[Keyless]
public class ReservationSummary
{
    public string RoomNumber { get; set; } = null!;
    public int ReservationsCount { get; set; }
    public decimal TotalAmount { get; set; }
}
