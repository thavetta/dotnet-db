namespace Bookings.Models;

public class Guest
{
    private string _name = null!;               // backing field (b)
    public Guid Id { get; private set; }
    public string Name                             // přes backing field
    {
        get => _name;
        private set => _name = value?.Trim() ?? throw new ArgumentNullException(nameof(Name));
    }
    public Email Email { get; private set; } = Email.Create("noreply@example.com"); // (e) converter + comparer v mapování
    public bool IsDeleted { get; private set; }     // (c) global filter
    public bool IsVip { get; private set; }         // (d) bool -> 'Y'/'N' converter
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // ctor with params (f)
    public Guest(Guid id, string name, Email email, bool isVip = false)
    {
        Id = id;
        Name = name;
        Email = email;
        IsVip = isVip;
    }

    private Guest() { } // EF

    public void Rename(string name) => Name = name;
    public void MarkDeleted() => IsDeleted = true;
    public void SetVip(bool value) => IsVip = value;
}
