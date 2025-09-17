using System.Reflection;
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
    public DbSet<Dictionary<string, object>> Settings => Set<Dictionary<string, object>>("Settings");
    

    public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }
    

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("bookings");

        // SP mapování

        

        b.Entity<Reservation>().UpdateUsingStoredProcedure("Reservation_Update", "dbo",
        spb =>
            {
                spb.HasOriginalValueParameter(r => r.Id);

                spb.HasParameter(r => r.CheckIn);
                spb.HasParameter(r => r.CheckOut);
                spb.HasParameter(r => r.Status);

                // optimistická konkurence
                spb.HasOriginalValueParameter(r => r.RowVersion);
                spb.HasResultColumn(r => r.RowVersion);
            });
        b.Entity<Reservation>().DeleteUsingStoredProcedure("Reservation_Delete","dbo",
        spb =>
            {
                spb.HasOriginalValueParameter(r => r.Id);
                spb.HasOriginalValueParameter(r => r.RowVersion);
            });

// DbFunction mapování

var method = typeof(Bookings.Infrastructure.MyDbFunctions).GetMethod(nameof(Bookings.Infrastructure.MyDbFunctions.Nights), new[] { typeof(DateTime), typeof(DateTime) });
b.HasDbFunction(method!).HasName("fn_Nights").HasSchema("dbo");


        // ------- Guest -------
        b.Entity<Guest>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
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
            // e.HasData(new Guest(
            //     id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            //     name: "Alice",
            //     email: Email.Create("alice@example.com"),
            //     isVip: true
            // ));
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
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Guest).WithMany().HasForeignKey(x => x.GuestId);

            e.InsertUsingStoredProcedure("Reservation_Insert","dbo", spb =>
            {
                spb.HasParameter(r => r.Id);
                spb.HasParameter(r => r.GuestId);
                spb.HasParameter(r => r.RoomId);
                
                spb.HasParameter(r => r.CheckIn);
                spb.HasParameter(r => r.CheckOut);
                spb.HasParameter(r => r.Status);
                spb.HasParameter(r => r.Amount);
                spb.HasParameter(r => r.Currency);
                
                spb.HasResultColumn(r => r.RowVersion);
            });

            e.UpdateUsingStoredProcedure("Reservation_Update", "dbo",
        spb =>
            {
                spb.HasOriginalValueParameter(r => r.Id);

                spb.HasParameter(r => r.CheckIn);
                spb.HasParameter(r => r.CheckOut);
                spb.HasParameter(r => r.Status);
                spb.HasParameter(r => r.Amount);
                spb.HasParameter(r => r.Currency);
                spb.HasParameter(r => r.GuestId);
                spb.HasParameter(r => r.RoomId);

                // optimistická konkurence
                spb.HasOriginalValueParameter(r => r.RowVersion);
                spb.HasResultColumn(r => r.RowVersion);
            });

            e.DeleteUsingStoredProcedure("Reservation_Delete","dbo",
        spb =>
            {
                spb.HasOriginalValueParameter(r => r.Id);
                spb.HasOriginalValueParameter(r => r.RowVersion);
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
                Amount = 120m,
                Currency = "EUR",
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
