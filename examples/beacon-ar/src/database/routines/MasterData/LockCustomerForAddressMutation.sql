CREATE PROCEDURE [dbo].[LockCustomerForAddressMutation]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @LockedId INT = 0;
    SELECT @LockedId = [Id]
    FROM [dbo].[Customer] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Id] = @Id;
    SELECT @LockedId;
END;
