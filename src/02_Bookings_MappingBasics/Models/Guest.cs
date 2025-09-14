namespace Bookings.Models;

public class Guest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public Email Email { get; set; } = Email.Create("n/a@example.com");
    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}