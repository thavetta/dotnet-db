namespace Bookings.Models;

public record Money(decimal Amount, string Currency)
{
    public static Money Zero(string c) => new(0m, c);
}