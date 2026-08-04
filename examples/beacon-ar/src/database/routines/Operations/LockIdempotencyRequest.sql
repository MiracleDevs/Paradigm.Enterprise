CREATE PROCEDURE [dbo].[LockIdempotencyRequest]
    @UserId INT,
    @Operation NVARCHAR(150),
    @KeyHash BINARY(32)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Id BIGINT = 0;
    SELECT @Id = [Id]
    FROM [dbo].[IdempotencyRequest] WITH (UPDLOCK, HOLDLOCK)
    WHERE [UserId] = @UserId AND [Operation] = @Operation AND [KeyHash] = @KeyHash;
    SELECT @Id;
END;
