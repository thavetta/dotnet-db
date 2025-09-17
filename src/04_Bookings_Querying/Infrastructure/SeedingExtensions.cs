using System.Diagnostics;
using Bogus;
using Bookings.Contexts;
using Bookings.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure;

public static class SeedingExtensions
{
    public static WebApplication UseSeeding(this WebApplication app)
    {
        app.MapPost("/seed", async (Bookings.Contexts.BookingsDbContext db, int guests = 200, int rooms = 50, int reservations = 500) =>
        {
        await db.Database.MigrateAsync();

        if (await db.Guests.AnyAsync() || await db.Rooms.AnyAsync() || await db.Reservations.AnyAsync())
            return Results.Ok(new { ok = true, skipped = true });

        var sw = Stopwatch.StartNew();
        //await using var tx = await db.Database.BeginTransactionAsync();

        // Rooms (TPC: StandardRoom/Suite)
        var stdRooms = new Faker<StandardRoom>("en")
            .CustomInstantiator(f => StandardRoom.Create(
                f.Random.Int(100, 499).ToString(),
                f.Random.Int(1, 3)
                ))
            .Generate(Math.Max(1, rooms * 3 / 4));

        var suites = new Faker<Suite>("en")
            .CustomInstantiator(f => Suite.Create(
                f.Random.Int(500, 899).ToString(),
                f.Random.Int(2, 6),
                f.Random.Bool(0.5f)))
            .Generate(Math.Max(1, rooms - stdRooms.Count));

        await db.AddRangeAsync(stdRooms);
        await db.AddRangeAsync(suites);
        await db.SaveChangesAsync();

        // Guests
            var guestFaker = new Faker<Guest>("en")
            .CustomInstantiator(f => new Guest(
                Guid.NewGuid(),
                f.Name.FullName(),
                Email.Create(f.Internet.Email()),
                f.Random.Bool(0.1f)));

        var guestsList = guestFaker.Generate(guests);
        await db.AddRangeAsync(guestsList);

        // Reservations
        var allRooms = stdRooms.Cast<Room>().Concat(suites).ToList();
        var resFaker = new Faker<Reservation>("en")
            .CustomInstantiator(f =>
            {
                var from = DateTime.UtcNow.Date.AddDays(-f.Random.Int(0, 120));
                var nights = f.Random.Int(1, 10);
                var to = from.AddDays(nights);
                var room = f.PickRandom(allRooms);
                var guest = f.PickRandom(guestsList);
                var price = (decimal)nights * 100;
                var total = Math.Round(price, 2);
                return Reservation.Create(
                    Guid.NewGuid(),
                    guest.Id,
                    room.Id,
                    from,
                    to,
                    total,"EUR");
            })
            .FinishWith((f, r) => r.Status = f.PickRandom<ReservationStatus>())
                                .Generate(reservations);

            await db.AddRangeAsync(resFaker);

            await db.SaveChangesAsync();
            //await tx.CommitAsync();
            sw.Stop();

            return Results.Ok(new { ok = true, inserted = new { guests = guestsList.Count, rooms = allRooms.Count, reservations = resFaker.Count }, ms = sw.ElapsedMilliseconds });
        });

        return app;
    }
}
