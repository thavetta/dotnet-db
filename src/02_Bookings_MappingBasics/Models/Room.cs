namespace Bookings.Models;

public abstract class Room
{
    public int Id { get; set; }
    public string Number { get; set; } = default!;
    public int Capacity { get; set; }
}