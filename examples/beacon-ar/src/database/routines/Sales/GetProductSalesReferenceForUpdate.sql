CREATE PROCEDURE [dbo].[GetProductSalesReferenceForUpdate]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [Sku], [Name], [IsActive]
    FROM [dbo].[Product] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Id] = @Id;
END;
