using Bookings.Domain;
using Bookings.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("SqlServer") ?? "Server=localhost;Database=Demo04_Bookings_Querying;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<BookingsDbContext>(o => o.UseSqlServer(cs).EnableRetryOnFailure().EnableSensitiveDataLogging());
var app = builder.Build();
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

// Querying endpoints
app.MapGet("/reservations", async (BookingsDbContext db) =>
    await db.Reservations.AsNoTracking()
        .Select(r => new { r.Id, Room = r.Room.Number, Guest = r.Guest.Name, r.CheckIn, r.CheckOut, r.Status, r.Price.Amount, r.Price.Currency })
        .ToListAsync());

app.MapGet("/rooms/{minCapacity:int}", async (BookingsDbContext db, int minCapacity) =>
    await db.Rooms.AsNoTracking().Where(r => r.Capacity >= minCapacity).Select(r => new { r.Id, r.Number, r.Capacity }).ToListAsync());

// Global filter demo: soft delete Guest
app.MapDelete("/guests/{id:guid}", async (BookingsDbContext db, Guid id) =>
{
    var g = await db.Guests.FindAsync(id);
    if (g is null) return Results.NotFound();
    g.IsDeleted = true;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();