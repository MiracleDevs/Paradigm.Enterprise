CREATE PROCEDURE [dbo].[GetCustomerSalesReferenceForUpdate]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [AccountNumber], [Name], [Email], [Phone], [IsActive]
    FROM [dbo].[Customer] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Id] = @Id;
END;
