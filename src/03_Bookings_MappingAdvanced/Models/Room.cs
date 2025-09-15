namespace Bookings.Models;

public abstract class Room
{
    public int Id { get; private set; }
    public string Number { get; protected set; } = null!;
    public int Capacity { get; protected set; }
}
