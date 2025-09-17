using Bookings.Contexts;
using Bookings.Dtos;
using Bookings.Infrastructure;
using Bookings.Models;
using Bookings.Queries;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Endpoints;

public static class Lab4Endpoints
{
    public static WebApplication MapLab4Endpoints(this WebApplication app)
    {
        // Eager
        app.MapGet("/reservations/eager", async (BookingsDbContext db) =>
        {
            var data = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(data);
        });

        // Lazy (vyžaduje UseLazyLoadingProxies a virtual navigace)
        app.MapGet("/reservations/lazy", async (BookingsDbContext db) =>
        {
            var list = await db.Reservations.AsNoTracking().ToListAsync();
            var shaped = list.Select(r => new {
                r.Id,
                Guest = r.Guest?.Name,
                Room = r.Room?.Number
            });
            return Results.Ok(shaped);
        });

        // Compiled query
        app.MapGet("/reservations/by-email", (BookingsDbContext db, string email) =>
        {
            return Results.Ok(Compiled.ReservationsByGuestEmail(db, email));
        });

        // CRUD přes SP (nutno mapovat v OnModelCreating)
        app.MapPost("/reservations", async (BookingsDbContext db, ReservationCreateDto dto) =>
        {
            var res = Reservation.Create(Guid.NewGuid(), dto.GuestId, dto.RoomId, dto.From, dto.To, dto.Total, dto.Currency);
            db.Add(res);
            await db.SaveChangesAsync();
            return Results.Created($"/reservations/{res.Id}", new { res.Id });
        });

        app.MapPut("/reservations/{id}", async (BookingsDbContext db, Guid id, ReservationUpdateDto dto) =>
        {
            var res = await db.Reservations.FindAsync(id);
            if (res is null) return Results.NotFound();
        res.CheckIn = dto.From;
        res.CheckOut = dto.To;
            res.Status = dto.Status;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/reservations/{id}", async (BookingsDbContext db, Guid id) =>
        {
            var res = await db.Reservations.FindAsync(id);
            if (res is null) return Results.NotFound();
            db.Remove(res);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Full vs Split
        app.MapGet("/reservations/full", async (BookingsDbContext db) =>
        {
            var data = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(data);
        });

        app.MapGet("/reservations/full-split", async (BookingsDbContext db) =>
        {
            var data = await db.Reservations
                .Include(r => r.Guest)
                .Include(r => r.Room)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync();
            return Results.Ok(data);
        });

        // DbFunction demo
        app.MapGet("/stats/top-longstays", async (BookingsDbContext db, int minNights) =>
        {
            var q = db.Reservations
                .Select(r => new {
                    r.Id,
                    Guest = r.Guest.Name,
                    Nights = MyDbFunctions.Nights(r.CheckIn.Date, r.CheckOut.Date),
                    Total = r.Amount
                })
                .Where(x => x.Nights >= minNights)
                .OrderByDescending(x => x.Nights)
                .Take(20);

            return Results.Ok(await q.AsNoTracking().ToListAsync());
        });

        return app;
    }
}
