namespace Bookings.Models;

public class Reservation
{
    public Guid Id { get; set; }
    public int RoomId { get; set; }
    public Guid GuestId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public Money Price { get; set; } = Money.Zero("EUR");
    public ReservationStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Room Room { get; set; } = default!;
    public Guest Guest { get; set; } = default!;
}
