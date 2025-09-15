using System.Collections.Generic;
using Bookings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bookings.Contexts;

public class BookingsDbContext : DbContext
{
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();             // TPC základ
    public DbSet<StandardRoom> StandardRooms => Set<StandardRoom>();
    public DbSet<Suite> Suites => Set<Suite>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationSummary> ReservationSummaries => Set<ReservationSummary>(); // (g) view

    // (i) Shared-type entities: UserSettings/SystemSettings mapované na jednu tabulku
    public DbSet<Dictionary<string, object>> UserSettings => Set<Dictionary<string, object>>("UserSettings");
    public DbSet<Dictionary<string, object>> SystemSettings => Set<Dictionary<string, object>>("SystemSettings");

    public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("bookings");

        // ------- Guest -------
        b.Entity<Guest>(e =>
        {
            e.Property<DateTimeOffset>("CreatedAt").HasColumnType("datetimeoffset(7)").HasDefaultValueSql("SYSDATETIMEOFFSET()"); // (h) shadow property
            e.Property<DateTimeOffset?>("UpdatedAt").HasColumnType("datetimeoffset(7)"); // (h) shadow property

            e.Property(x => x.RowVersion).IsRowVersion();

            // (b) backing field pro Name
            e.Property(x => x.Name).HasMaxLength(200).IsRequired().HasField("_name");
            e.HasIndex(x => x.Name);

            // (c) global filter IsDeleted
            e.HasQueryFilter(g => !g.IsDeleted);

            // (d) Converter bool -> 'Y'/'N'
            var yesNoConverter = new ValueConverter<bool, string>(
                v => v ? "Y" : "N",
                v => v == "Y");
            e.Property(x => x.IsVip)
                .HasConversion(yesNoConverter)
                .HasMaxLength(1)
                .HasColumnName("IsVipYN");

            // (e) Email přes ValueConverter + ValueComparer (case-insensitive)
            var emailConverter = new ValueConverter<Email, string>(
                v => v.Value,
                v => Email.Create(v));

            var emailComparer = new ValueComparer<Email>(
                (a, b) => string.Equals(a.Value, b.Value, StringComparison.OrdinalIgnoreCase),
                v => StringComparer.OrdinalIgnoreCase.GetHashCode(v.Value),
                v => Email.Create(v.Value));

            e.Property(x => x.Email)
                .HasConversion(emailConverter)
                .Metadata.SetValueComparer(emailComparer);

            // seed (j) – Guest
            e.HasData(new Guest(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                name: "Alice",
                email: Email.Create("alice@example.com"),
                isVip: true
            ));
        });

        // ------- Room (TPC) -------
        b.Entity<Room>(e =>
        {
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.UseTpcMappingStrategy();
        });

        b.Entity<StandardRoom>(e =>
        {
            e.ToTable("StandardRooms");
            e.Property(x => x.Capacity).HasDefaultValue(2);
            e.HasData(new { Id = 1, Number = "101", Capacity = 2 });
        });

        b.Entity<Suite>(e =>
        {
            e.ToTable("Suites");
            e.Property(x => x.HasLounge).HasDefaultValue(false);
            e.HasData(new { Id = 2, Number = "101", Capacity = 2, HasLounge = true });
        });

        // ------- Reservation -------
        b.Entity<Reservation>(e =>
        {
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Guest).WithMany().HasForeignKey(x => x.GuestId);

            // Owned Money (dvě kolony) + seed owned dat (j)
            e.OwnsOne(x => x.Price, o =>
            {
                o.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                o.Property(p => p.Currency).HasMaxLength(3).IsRequired();
                o.HasData(new
                {
                    ReservationId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Amount = 120m,
                    Currency = "EUR"
                });
            });

            e.HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });

            // seed (j) – Reservation s FK na Guest/Room
            e.HasData(new
            {
                Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                RoomId = 1,
                GuestId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                CheckIn = new DateTime(2025, 01, 10),
                CheckOut = new DateTime(2025, 01, 12),
                Status = ReservationStatus.Confirmed
            });
        });

        // ------- (g) Keyless entita navázaná na VIEW -------
        b.Entity<ReservationSummary>(e =>
        {
            e.ToView("vwReservationSummary");
            e.HasNoKey();
        });

        b.SharedTypeEntity<Dictionary<string, object>>("Settings", e =>
        {
            e.ToTable("Settings", "bookings");
            e.IndexerProperty<int>("Id");
            e.IndexerProperty<string>("Key").HasMaxLength(100).IsRequired();
            e.IndexerProperty<string>("Value").HasMaxLength(1000).IsRequired();
            e.HasKey("Id");
        });

        
        // seed (j) – Settings
        b.SharedTypeEntity<Dictionary<string, object>>("Settings")
            .HasData(new Dictionary<string, object>
            {
                ["Id"] = 1,
                ["Key"] = "RetentionDays",
                ["Value"] = "90",
            });
        b.SharedTypeEntity<Dictionary<string, object>>("Settings")
            .HasData(new Dictionary<string, object>
            {
                ["Id"] = 2,
                ["Key"] = "Theme",
                ["Value"] = "dark",
            });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyShadowTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyShadowTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // (h) obsluha shadow property CreatedAt/UpdatedAt
    private void ApplyShadowTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State == EntityState.Added && e.Metadata.FindProperty("CreatedAt") is not null 
                && e.Property("CreatedAt").CurrentValue is null)
            {
                e.Property("CreatedAt").CurrentValue = now;
            }
            if (e.State == EntityState.Modified && e.Metadata.FindProperty("UpdatedAt") is not null)
            {
                e.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }


}
