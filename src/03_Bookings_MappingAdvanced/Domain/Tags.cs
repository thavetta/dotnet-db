namespace Bookings.Domain;
public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}
public class GuestTag
{
    public Guid GuestId { get; set; }
    public int TagId { get; set; }
}
