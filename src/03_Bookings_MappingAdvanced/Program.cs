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
