namespace Bookings.Models;

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string c = "EUR") => new(0m, c);

    public static Money Create(decimal amount, string currency)
        => new(amount, currency);
}
