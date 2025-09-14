using Bookings.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure;

public class BookingsDbContext : DbContext
{
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("bookings");
        b.Entity<Guest>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => x.Name);
            e.OwnsOne(x => x.Email, o =>
            {
                o.Property(p => p.Value).HasColumnName("Email").HasMaxLength(320).IsRequired();
            });
        });

        b.Entity<Room>(e =>
        {
            e.ToTable("Rooms");
            e.HasDiscriminator<string>("RoomType")
                .HasValue<StandardRoom>("standard")
                .HasValue<Suite>("suite");
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
        });

        b.Entity<Reservation>(e =>
        {
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Guest).WithMany(g => g.Reservations).HasForeignKey(x => x.GuestId);
            e.OwnsOne(x => x.Price, o =>
            {
                o.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                o.Property(p => p.Currency).HasMaxLength(3).IsRequired();
            });
            e.HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });
        });

        b.Entity<Tag>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        b.Entity<GuestTag>().HasKey(x => new { x.GuestId, x.TagId });
        b.Entity<Guest>()
            .HasMany<GuestTag>()
            .WithOne()
            .HasForeignKey(gt => gt.GuestId);
        b.Entity<Tag>()
            .HasMany<GuestTag>()
            .WithOne()
            .HasForeignKey(gt => gt.TagId);
    }
}