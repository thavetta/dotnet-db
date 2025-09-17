namespace Bookings.Models;

public class Reservation
{
    // ctor s parametry (f)
    public Reservation(Guid id, int roomId, Guid guestId, DateTime checkIn, DateTime checkOut, decimal amount, ReservationStatus status)
    {
        Id = id;
        RoomId = roomId;
        GuestId = guestId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Amount = amount;
        Status = status;
    }

    public static Reservation Create(Guid id, Guid guestId, int roomId, DateTime checkIn, DateTime checkOut, decimal amount)
        => new Reservation(id, roomId, guestId, checkIn, checkOut, amount, ReservationStatus.Pending);

    private Reservation() { } // EF

    public Guid Id { get; private set; }
    public int RoomId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateTime CheckIn { get;  set; }
    public DateTime CheckOut { get;  set; }
    public decimal Amount { get; set; } 
    public string Currency { get; set; } = "EUR";
    public ReservationStatus Status { get; set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Room Room { get; private set; } = null!;
    public Guest Guest { get; private set; } = null!;
}
