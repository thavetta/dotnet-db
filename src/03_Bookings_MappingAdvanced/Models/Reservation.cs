namespace Bookings.Models;

public class Reservation
{
    // ctor s parametry (f)
    public Reservation(Guid id, int roomId, Guid guestId, DateTime checkIn, DateTime checkOut, Money price, ReservationStatus status)
    {
        Id = id;
        RoomId = roomId;
        GuestId = guestId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Price = price;
        Status = status;
    }

    private Reservation() { } // EF

    public Guid Id { get; private set; }
    public int RoomId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateTime CheckIn { get; private set; }
    public DateTime CheckOut { get; private set; }
    public Money Price { get; private set; } = Money.Zero();
    public ReservationStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Room Room { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;
}
