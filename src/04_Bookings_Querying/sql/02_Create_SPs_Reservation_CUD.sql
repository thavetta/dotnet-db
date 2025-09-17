CREATE OR ALTER PROCEDURE [dbo].[Reservation_Insert]
    @Id uniqueidentifier,
    @GuestId uniqueidentifier,
    @RoomId int,
    @From date,
    @To date,
    @Status nvarchar(20),
    @Amount decimal(18,2),
    @Currency nvarchar(3)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO bookings.Reservations(Id, GuestId, RoomId, CheckIn, CheckOut, [Status], Amount, Currency)
    VALUES (@Id, @GuestId, @RoomId, @From, @To, @Status, @Amount, @Currency);

	SELECT RowVersion
  FROM bookings.Reservations
  WHERE Id = @Id; 
END
GO

CREATE OR ALTER PROCEDURE dbo.Reservation_Update
    @Id uniqueidentifier,
    @CheckIn datetime2,
    @CheckOut datetime2,
    @Status nvarchar(20),
    @RowVersion varbinary(8),   -- původní hodnota z klienta
    @Amount decimal(18,2),
    @Currency nvarchar(3),
    @GuestId uniqueidentifier,
    @RoomId int
    
AS
BEGIN
  SET NOCOUNT ON;

  UPDATE bookings.Reservations
     SET CheckIn = @CheckIn,
         CheckOut = @CheckOut,
         Status = @Status,
         Amount = @Amount,
         Currency = @Currency,
         GuestId = @GuestId,
         RoomId = @RoomId
   WHERE Id = @Id AND RowVersion = @RowVersion;

  -- pro HasResultColumn(RowVersion)
  SELECT RowVersion
  FROM bookings.Reservations
  WHERE Id = @Id;
END

GO

CREATE OR ALTER PROCEDURE dbo.Reservation_Delete
    @Id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM bookings.Reservations WHERE Id=@Id;
END
GO
