using Bookings.Contexts;
using Bookings.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Queries;

public static class Compiled
{
    public static readonly Func<BookingsDbContext, string, IAsyncEnumerable<Reservation>> ReservationsByGuestEmail =
        EF.CompileAsyncQuery((BookingsDbContext db, string email) =>
            db.Reservations
              .Where(r => r.Guest.Email.Value == email)
              .Include(r => r.Room)
              .OrderByDescending(r => r.CheckIn)
              .AsQueryable());
}
