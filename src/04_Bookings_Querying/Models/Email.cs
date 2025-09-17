namespace Bookings.Models;

public sealed record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ArgumentException("Invalid email", nameof(value));
        return new Email(value.Trim());
    }
    public override string ToString() => Value;
}
