# 02 – Bookings (Mapping Basics)

**Cíl:** Založit EF Core model *Bookings*, vysvětlit mapování a připravit minimální API. Níže má každý soubor vlastní sekci s krátkým popisem a **kompletním kódem**.

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
    Value object pro e‑mail. Slouží jako typová zábrana a jednoduchá validace. V DB ho mapujeme jako **owned type** (viz DbContext).

    ```csharp
    namespace Bookings.Models;

    public record Email(string Value)
    {
        public static Email Create(string value)
            => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
                ? throw new ArgumentException("Invalid email")
                : new Email(value.Trim());
        public override string ToString() => Value;
    }
    ```

1. **Models/Money.cs**

    Value object pro cenu. V tomhle labu ho ukládáme do `Reservation` jako **owned typ** se sloupci `Amount` a `Currency`. Decimal sloupec je definovaný jako `decimal(18,2)`. 

    ```csharp
    namespace Bookings.Models;

    public record Money(decimal Amount, string Currency)
    {
        public static Money Zero(string c) => new(0m, c);
    }
    ```

1. **Models/ReservationStatus.cs**

    Výčet stavů rezervace. V DB se ukládá jako **string** (přes `HasConversion<string>()`), což pomáhá čitelnosti dat a umožní přidat stav bez re‑seedu lookup tabulky.

    ```csharp
    namespace Bookings.Models;

    public enum ReservationStatus 
    { 
        Pending,
        Confirmed,
        Cancelled,
        CheckedIn,
        CheckedOut
    }
    ```

1. **Models/Room.cs**

    Abstraktní základ pro pokoje. V DB je mapován jako **TPH** (Table‑Per‑Hierarchy) se sloupcem diskriminátoru `RoomType`.

    ```csharp
    namespace Bookings.Models;

    public abstract class Room
    {
        public int Id { get; set; }
        public string Number { get; set; } = default!;
        public int Capacity { get; set; }
    }
    ```

1. **Models/StandardRoom.cs**

    Konkrétní typ pokoje bez dalších vlastností. V TPH sdílí tabulku s `Suite`. 

    ```csharp
    namespace Bookings.Models;

    public class StandardRoom : Room
    {
    }
    ```

1. **Models/Suite.cs**

    Druhá větev dědičnosti s doplňkovým polem `HasLounge`. 

    ```csharp
    namespace Bookings.Models;

    public class Suite : Room 
    { 
        public bool HasLounge { get; set; } 
    }
    ```

1. **Models/Guest.cs**

    Host – základní entita s `Email` (owned), `RowVersion` pro **optimistickou konkurenci** a navigací na `Reservations`. 

    ```csharp
    namespace Bookings.Models;

    public class Guest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public Email Email { get; set; } = Email.Create("n/a@example.com");
        public bool IsDeleted { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
    ```

1. **Models/Reservation.cs**

    Rezervace s odkazy na `Room` a `Guest`. Obsahuje **`Price: Money` (owned)**, **`Status: ReservationStatus` (enum → string)** a `RowVersion`.

    ```csharp
    namespace Bookings.Models;

    public class Reservation
    {
        public Guid Id { get; set; }
        public int RoomId { get; set; }
        public Guid GuestId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public Money Price { get; set; } = Money.Zero("EUR");
        public ReservationStatus Status { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Room Room { get; set; } = default!;
        public Guest Guest { get; set; } = default!;
    }
    ```

1. Jakmile je hotovo vše v Models, lze nachystat DbContext který ty všechny třídy vzájemně sváže a přidá informace nutné pro databázi.
1. **Contexts/BookingsDbContext.cs**

    Základní třída kontextu. Nastavuje **výchozí schéma `bookings`** a definuje sady entit.

    ```csharp
    using Bookings.Models;
    using Microsoft.EntityFrameworkCore;

    namespace Bookings.Contexts;

    public class BookingsDbContext : DbContext
    {
        public DbSet<Guest> Guests => Set<Guest>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options) { }
    }
    ```

1. **Mapování entit** – co se nastavuje

    1. **Guest**  

        - `Name` povinné + index.  
        - `RowVersion` jako `rowversion` pro optimistic concurrency.  
        - `Email` jako **owned type** do jednoho sloupce `Email` (max 320).

    1. **Room (TPH)**  

        - Jedna tabulka `Rooms` pro všechny potomky.  
        - Discriminátor `RoomType` s hodnotami `standard`/`suite`.  
        - `Number` povinné, max 20.

    1. **Reservation**  

        - `RowVersion` jako `rowversion`.  
        - `Status` ukládán jako **string** (max 20).  
        - FK na `Room` (Restrict delete) a na `Guest`.  
        - **Owned `Price`** → `Amount decimal(18,2)`, `Currency` (max 3).  
        - Index na `(RoomId, CheckIn, CheckOut)` pro rychlé hledání kolizí.

1. **Kompletní kód DbContextu**

    ```csharp
    using Bookings.Models;
    using Microsoft.EntityFrameworkCore;

    namespace Bookings.Contexts;

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
        }
    }
    ```

1. Dalším krokem je využít MinimalAPI pro konfiguraci a běh aplikace
1. Do **program.cs** se nejdřív přidá kód pro získání connection stringu, pak se registruje db provider
1. Pokud aplikace běží v *Developer* prostředí, nastaví se pár specialit a hlavně se automaticky spustí Migrace, která zaktualizuje struktury DB

    ```csharp
    using Bookings.Models;
    using Bookings.Contexts;
    using Microsoft.EntityFrameworkCore;

    var builder = WebApplication.CreateBuilder(args);
    var cs = builder.Configuration.GetConnectionString("SqlServer") ?? "Server=localhost;Database=02_Bookings_MappingBasics;Trusted_Connection=True;TrustServerCertificate=True;";
    builder.Services
        .AddDbContext<BookingsDbContext>(o => o.UseSqlServer(cs, sql => sql.EnableRetryOnFailure())
        .EnableSensitiveDataLogging()
        );
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {

        app.UseDeveloperExceptionPage();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
        db.Database.Migrate();
    }
    ```

1. Aplikace mapuje pouze dvě URL

    - `GET /health` — kontrola běhu.  
    - `POST /seed` — idempotentní nasypání ukázkových dat (`Rooms`, `Guest`). **Pozor: POST**, aby nebyl side‑effect na GET.

    ```csharp
    app.MapGet("/health", () => Results.Ok(new { ok = true }));
    app.MapPost("/seed", async (BookingsDbContext db) =>
    {
        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(new StandardRoom { Number = "101", Capacity = 2 }, new Suite { Number = "501", Capacity = 4, HasLounge = true });
            await db.SaveChangesAsync();
        }
        if (!await db.Guests.AnyAsync())
        {
            var g = new Guest { Id = Guid.NewGuid(), Name = "Alice", Email = Email.Create("alice@example.com") };
            db.Guests.Add(g);
            await db.SaveChangesAsync();
        }
        return Results.Ok();
    });

    app.Run();
    ```

1. Další nezbytný krok je vytvoření migrace, která se postaré při spuštění aplikace o vytvoření DB, pokud neexistuje
1. Pro fungování je potřeba jednou na PC spustit instalaci potřebných toolů (pokud tam nejsou) **EF CLI** (jednorázově):  

   ```bash
   dotnet tool update --global dotnet-ef
   ```

1. **Vygenerujte migraci** (spouštějte v adresáři projektu `02_Bookings_MappingBasics`):  

   ```bash
   dotnet ef migrations add StartDBMig -o Contexts/Migrations
   ```

1. Pokud nechcete migraci spouštět automaticky z aplikace (doporučeno pouze pro vývoj, ne pro produkci), můžete migraci spustit opět jako příkaz:  

   ```bash
   dotnet ef database update
   ```

1. **Spuštění a kontrola**

   ```bash
   dotnet run
   curl http://localhost:5000/health
   curl -X POST http://localhost:5000/seed
   ```

   - V logu by měly být vidět `INSERT` do `bookings.Rooms` a `bookings.Guests`.
   - V SSMS ověřte (správné **schema**!):

     ```sql
     SELECT COUNT(*) FROM [bookings].[Rooms];
     SELECT COUNT(*) FROM [bookings].[Guests];
     ```

**Kontrola:** Tabulky vzniknou ve schématu `bookings`. První `POST /seed` vloží vzorky dat; v logu uvidíte `INSERT` do `bookings.Rooms` a `bookings.Guests`.
