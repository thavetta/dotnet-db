using Bookings.Domain;
using Bookings.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("SqlServer") ?? "Server=localhost;Database=Demo05_Bookings_SaveChanges_Migrations;Trusted_Connection=True;TrustServerCertificate=True;";
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

// Concurrency demo: update reservation with rowversion
app.MapPut("/reservations/{id:guid}/price", async (BookingsDbContext db, Guid id, UpdatePriceDto dto) =>
{
    var r = await db.Reservations.FirstOrDefaultAsync(x => x.Id == id);
    if (r is null) return Results.NotFound();
    r.Price = new Money(dto.Amount, dto.Currency);
    try
    {
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Results.Conflict(new { message = "Concurrency conflict. Reload and retry." });
    }
});

// Transaction demo
app.MapPost("/import", async (BookingsDbContext db) =>
{
    await using var tx = await db.Database.BeginTransactionAsync();
    try
    {
        db.Rooms.Add(new StandardRoom { Number = "302", Capacity = 2 });
        db.Rooms.Add(new StandardRoom { Number = "303", Capacity = 2 });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Ok();
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
});

record UpdatePriceDto(decimal Amount, string Currency);

app.Run();