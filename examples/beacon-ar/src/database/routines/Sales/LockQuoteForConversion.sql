CREATE PROCEDURE [dbo].[LockQuoteForConversion]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LockedId INT = 0;
    SELECT @LockedId = [Id]
    FROM [dbo].[Quote] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Id] = @Id AND [DeletionDate] IS NULL;
    SELECT @LockedId;
END;
