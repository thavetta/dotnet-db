CREATE OR ALTER FUNCTION dbo.fn_Nights(@from date, @to date)
RETURNS int
AS
BEGIN
    RETURN DATEDIFF(DAY, @from, @to);
END
GO
