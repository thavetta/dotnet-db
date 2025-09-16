# 03 – Bookings (Mapping Advanced)

**Cíl:** Pokročilé mapování v EF Core na jednom projektu. Ukážeme: **TPC (Table‑per‑Concrete class)** dědičnost pro `Room` (a), **backing field** (b), **globální filtr** (c), **ValueConverter** `bool ↔ 'Y'/'N'` (d), **ValueComparer** (e), **konstruktory s parametry** (f), **keyless entitu** na **VIEW** (g), **shadow properties** (h), **(jedna) shared entity**/nastavení (i) a **HasData** seed včetně FK a owned typu (j).

## Postup

1. Založte nový projekt typu Web App s Minimal API, .NET 9
1. Přidejte do projektu tyto nuget balíčky

    - Microsoft.EntityFrameworkCore
    - Microsoft.EntityFrameworkCore.SqlServer
    - Microsoft.EntityFrameworkCore.Design
    - Microsoft.EntityFrameworkCore.Tools

1. Do *appsettings.json* doplňte podobně jak v lab 1 connection string, tentokrát na neexistující DB. DB vytvoříme později na základě CodeFirst definice tabulek a struktur.
1. Do projektu přidejte složky **Models** a **Contexts** a do nich postupně soubory dle návodu

1. **Models/Email.cs**

    Value object pro e‑mail. V tomto labu je mapovaný **konvertorem** na `string` (ne jako owned), navíc s **ValueComparer** (case‑insensitive) v DbContextu.

    ```csharp
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

    ```

1. **Models/Money.cs**

    Value object pro cenu; v `Reservation` je jako **owned typ** do dvojice sloupců `Price_Amount`/`Price_Currency`.

    ```csharp
    namespace Bookings.Models;

    public sealed record Money(decimal Amount, string Currency)
    {
        public static Money Zero(string c = "EUR") => new(0m, c);
    }

    ```

1. **Models/ReservationStatus.cs**

    Výčet stavů rezervace. Ukládáme jako **string** kvůli čitelnosti dat (viz DbContext).

    ```csharp
    namespace Bookings.Models;

    public enum ReservationStatus
    {
        Pending, Confirmed, Cancelled, CheckedIn, CheckedOut
    }

    ```

1. **Models/Room.cs**

    Abstraktní základ pokojů. V rámci labu je hierarchie mapovaná jako **TPC** – potomci mají vlastní tabulky.

    ```csharp
    namespace Bookings.Models;

    public abstract class Room
    {
        public int Id { get; private set; }
        public string Number { get; protected set; } = null!;
        public int Capacity { get; protected set; }
    }

    ```

1. **Models/StandardRoom.cs**

    Konkrétní pokoj; v TPC má **tabulku `StandardRooms`**.

    ```csharp
    namespace Bookings.Models;

    public class StandardRoom : Room
    {
        public StandardRoom() { }
        public StandardRoom(string number, int capacity)
        {
            Number = number;
            Capacity = capacity;
        }
    }

    ```

1. **Models/Suite.cs**

    Druhá větev dědičnosti s vlastností `HasLounge`; v TPC má **tabulku `Suites`**.

    ```csharp
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
    }

    ```

1. **Models/Guest.cs**

    Host – ukazuje **backing field** pro `Name`, globální filtr `IsDeleted`, `RowVersion` a `Email` jako VO (konvertor + comparer).

    ```csharp
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
    ```

1. **Models/Reservation.cs**

Rezervace s FK na `Room` a `Guest`, **owned `Price: Money`**, `Status` jako string a **konstruktorem s parametry**.

    ```csharp
    namespace Bookings.Models;

    public class Reservation
    {
        // ctor s parametry (f)
        public Reservation(Guid id, int roomId, Guid guestId, DateTime checkIn, DateTime checkOut, Money price, ReservationStatus status)
        {
            Id = id;
            RoomId = roomId;
            GuestId = guestId;
            CheckIn = checkIn;
            CheckOut = checkOut;
            Price = price;
            Status = status;
        }

        private Reservation() { } // EF

        public Guid Id { get; private set; }
        public int RoomId { get; private set; }
        public Guid GuestId { get; private set; }
        public DateTime CheckIn { get; private set; }
        public DateTime CheckOut { get; private set; }
        public Money Price { get; private set; } = Money.Zero();
        public ReservationStatus Status { get; private set; }
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        public Room Room { get; private set; } = null!;
        public Guest Guest { get; private set; } = null!;
    }

    ```

1. **Models/ReservationSummary.cs**

    **Keyless** typ navázaný na databázové **VIEW** `vwReservationSummary`.

    ```csharp
    using Microsoft.EntityFrameworkCore;

    namespace Bookings.Models;

    // (g) Keyless entita navázaná na VIEW
    [Keyless]
    public class ReservationSummary
    {
        public string RoomNumber { get; set; } = null!;
        public int ReservationsCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    ```

1. **Contexts/BookingsDbContext.cs** — hlavička a DbSety

    Nastaví **schema `bookings`** a deklaruje všechny `DbSet`y včetně keyless entity pro view.

    ```csharp
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
    }
    ```

1. **Mapování entit**

    1. **Room (TPC)**  

        - `UseTpcMappingStrategy()` pro hierarchii `Room`.  
        - `StandardRoom` → `StandardRooms`, `Suite` → `Suites`.  
        - Seed přes `HasData` probíhá anonymními objekty, aby nebyly potřeba public settery na PK (viz TPC + enkapsulace).

    1. **Guest**  

        - `Name` s **backing field** (mapováno přes `HasField`), index na jméno.  
        - **Globální filtr**: `!IsDeleted` – smazané záznamy se z běžných dotazů automaticky vyfiltrují.  
        - `IsVip` má **ValueConverter bool → 'Y'/'N'** (`IsVipYN`).  
        - `Email` je **ValueObject** mapovaný přes **ValueConverter** na `string` + **ValueComparer** pro case‑insensitive porovnání.  
        - **Shadow properties** `CreatedAt/UpdatedAt` (bez CLR vlastnosti); plní se v `SaveChanges`.  
        - `RowVersion` pro optimistic concurrency.  
        - **Seed**: ukázkový host.

    1. **Reservation**  

        - FK na `Room` (DeleteBehavior.Restrict) a na `Guest`.  
        - `Status` ukládán jako **string** (`nvarchar(20)`).  
        - **Owned `Price: Money`** → `Price_Amount decimal(18,2)` a `Price_Currency nvarchar(3)`; seed owned části přes `ReservationId`.  
        - `RowVersion` + index `(RoomId, CheckIn, CheckOut)`.  
        - Konstruktor s parametry – EF ho využije při materializaci.

    1. **ReservationSummary (keyless view)**  

        - `ToView("vwReservationSummary")` + `HasNoKey()`. Samotné **CREATE VIEW** je dopsáno do migrace (viz níže).

    1. **Settings (shared)**  

        - Jedna tabulka `Settings` (shared entity / property bag) s klíčem `Id`, sloupci `Key`, `Value`.  
        
    1. **Kompletní kód DbContextu**

        ```csharp
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

        ```

1. **Program.cs** — inicializace

    - Načtení connection stringu a registrace `BookingsDbContext` (SQL Server + retry). V Dev prostředí je povolené **sensitivní logování** pro snadnější ladění.

    ```csharp
    using Bookings.Contexts;
    using Bookings.Models;
    using Microsoft.EntityFrameworkCore;

    var builder = WebApplication.CreateBuilder(args);

    var cs = builder.Configuration.GetConnectionString("SqlServer")
            ?? "Server=.\\SQLEXPRESS;Database=03_Bookings_MappingAdvanced;Trusted_Connection=True;TrustServerCertificate=True;";

    builder.Services.AddDbContext<BookingsDbContext>(o =>
        o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
        .EnableSensitiveDataLogging());
    var app = builder.Build();
    ```

1. **Program.cs** — mapování URL

    - `GET /health` — jednoduchý healthcheck.
    - `POST /seed` — idempotentní vložení ukázkových dat (pokoje, host, rezervace). **POST** záměrně kvůli side‑effectu.
    - `GET /rooms` — kontrola TPC (vrací separátně `StandardRooms` a `Suites`).

    ```csharp
    // Dev: automatická migrace
    if (app.Environment.IsDevelopment())
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
            await db.Database.MigrateAsync();
        }

    app.MapGet("/health", () => Results.Ok(new { ok = true }));

    // (seed) POST /seed – naplnění dat, idempotentní
    app.MapPost("/seed", async (BookingsDbContext db) =>
    {
        var inserted = 0;
        if (!await db.Guests.AnyAsync())
        {
            db.Add(new Guest(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Alice", Email.Create("alice@example.com"), isVip: true));
            inserted++;
        }
        if (!await db.Reservations.AnyAsync())
        {
            db.Add(new Reservation(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                roomId: 1,
                guestId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                checkIn: new DateTime(2025, 1, 10),
                checkOut: new DateTime(2025, 1, 12),
                price: new Money(120m, "EUR"),
                status: ReservationStatus.Confirmed
            ));
            inserted++;
        }
        await db.SaveChangesAsync();
        return Results.Ok(new { inserted });
    });

    // jednoduché dotazy pro ověření TPC a filtrů
    app.MapGet("/rooms", async (BookingsDbContext db) =>
    {
        var std = await db.StandardRooms.AsNoTracking().Select(r => new { r.Id, r.Number, r.Capacity }).ToListAsync();
        var suites = await db.Suites.AsNoTracking().Select(r => new { r.Id, r.Number, r.Capacity, Type = "suite" }).ToListAsync();
        return Results.Ok(new { std, suites });
    });

    app.Run();
    ```

---

## Migrace, VIEW a spuštění

1. **EF CLI** (pouze pokud jste nedělal předešlý lab):

   ```bash
   dotnet tool update --global dotnet-ef
   ```

1. **Vygeneruj migraci** (v adresáři projektu `03_Bookings_MappingAdvanced`):

   ```bash
   dotnet ef migrations add AdvancedMapping_Start -o Contexts/Migrations
   ```

1. **Aplikuj migraci**:

   ```bash
   dotnet ef database update
   ```

1. **Vygeneruj další prázdnou migraci** (v adresáři projektu `03_Bookings_MappingAdvanced`):

   ```bash
   dotnet ef migrations add AdvancedMapping_View -o Contexts/Migrations
   ```

1. **Doplň VIEW** do `Up()` (TPC ⇒ `UNION ALL` přes `StandardRooms` a `Suites`):

    ```csharp
    migrationBuilder.Sql(@"
            IF OBJECT_ID(N'[bookings].[vwReservationSummary]', N'V') IS NOT NULL
                DROP VIEW [bookings].[vwReservationSummary];
            ");

    migrationBuilder.Sql(@"
        CREATE VIEW [bookings].[vwReservationSummary] AS
        SELECT r.Number AS RoomNumber,
            COUNT(res.Id)          AS ReservationsCount,
            SUM(res.Price_Amount)  AS TotalAmount
        FROM (
            SELECT Id, RoomId, Price_Amount
            FROM   [bookings].[Reservations]
        ) res
        JOIN (
            SELECT Id, Number FROM [bookings].[StandardRooms]
            UNION ALL
            SELECT Id, Number FROM [bookings].[Suites]
        ) r ON r.Id = res.RoomId
        GROUP BY r.Number;
    ");
    ```

   A do `Down()` přidej

    ```csharp
    migrationBuilder.Sql(@"IF OBJECT_ID(N'[bookings].[vwReservationSummary]', N'V') IS NOT NULL
                           DROP VIEW [bookings].[vwReservationSummary];");
    ```

1. **Podruhé aplikuj migraci**:

   ```bash
   dotnet ef database update
   ```

1. **Spusť a otestuj** (konkrétní porty viz. Properties\launchSettings.json):

   ```bash
   dotnet run
   curl http://localhost:5000/health
   curl -X POST http://localhost:5000/seed
   curl http://localhost:5000/rooms
   ```

### Poznámky

- **TPC** zapisuje sdílené vlastnosti (`Number`, `Capacity`) do **každé tabulky potomků** – tabulka `Rooms` pro kořen **nevzniká**.
- **Shadow properties** se při seedech („HasData“) nedoplňují přes `SaveChanges`, proto jsou v modelu buď `nullable`, nebo mají default, případně jsou součástí seedu.
- **Shared Settings** je ukázka „property‑bag“ entity; pokud chceš oddělené `UserSettings`/`SystemSettings`, použij TPH s běžnými CLR třídami (viz naše předchozí diskuze).
