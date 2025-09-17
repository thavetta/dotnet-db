namespace Bookings.Models;

public class StandardRoom : Room
{
    public StandardRoom() { }
    public StandardRoom(string number, int capacity)
    {
        Number = number;
        Capacity = capacity;
        
    }

    public static StandardRoom Create(string number, int capacity)
        => new StandardRoom(number, capacity);
}
