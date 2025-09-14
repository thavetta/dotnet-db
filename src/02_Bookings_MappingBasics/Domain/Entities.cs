namespace Bookings.Domain;

public record Email(string Value)
{
    public static Email Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? throw new ArgumentException("Invalid email")
            : new Email(value.Trim());
    public override string ToString() => Value;
}
public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string c) => new(0m, c);
}

public abstract class Room
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public int Capacity { get; set; }
}
public class StandardRoom : Room { }
public class Suite : Room { public bool HasLounge { get; set; } }

public class Guest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public Email Email { get; set; } = Email.Create("n/a@example.com");
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

public enum ReservationStatus { Pending, Confirmed, Cancelled, CheckedIn, CheckedOut }

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
