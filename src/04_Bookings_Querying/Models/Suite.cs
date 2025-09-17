namespace Bookings.Models;

public class Suite : Room
{
    public bool HasLounge { get; private set; }
    public Suite() { }
    public Suite(string number, int capacity, bool hasLounge)
    {
        Number = number;
        Capacity = capacity;
        HasLounge = hasLounge;
    }

    public static Suite Create(string number, int capacity, bool hasLounge)
        => new Suite(number, capacity, hasLounge);
}
