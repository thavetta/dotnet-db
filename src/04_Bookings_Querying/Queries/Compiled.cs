using Bookings.Contexts;
using Bookings.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Queries;

public static class Compiled
{
    public static readonly Func<BookingsDbContext, Email, IAsyncEnumerable<Reservation>> ReservationsByGuestEmail =
        EF.CompileAsyncQuery((BookingsDbContext db, Email email) =>
            db.Reservations
              .Where(r => r.Guest.Email == email)
              .Include(r => r.Room)
              .OrderByDescending(r => r.CheckIn)
              .AsQueryable());
}
