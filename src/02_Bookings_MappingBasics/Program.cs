using Bookings.Domain;
using Bookings.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var cs = builder.Configuration.GetConnectionString("SqlServer") ?? "Server=localhost;Database=Demo02_Bookings_MappingBasics;Trusted_Connection=True;TrustServerCertificate=True;";
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

app.Run();