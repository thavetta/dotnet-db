namespace Bookings.Models;

public record Email(string Value)
{
    public static Email Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? throw new ArgumentException("Invalid email")
            : new Email(value.Trim());
    public override string ToString() => Value;
}