using Bookings.Models;

namespace Bookings.Dtos;

public record ReservationCreateDto(Guid GuestId, int RoomId, DateTime From, DateTime To, decimal Total, string Currency);
public record ReservationUpdateDto(DateTime From, DateTime To, ReservationStatus Status);
public record MoneyDto(decimal Value, string Currency);
