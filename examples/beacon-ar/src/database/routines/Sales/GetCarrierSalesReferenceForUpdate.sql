CREATE PROCEDURE [dbo].[GetCarrierSalesReferenceForUpdate]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Id], [Name], [IsActive]
    FROM [dbo].[Carrier] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Id] = @Id;
END;
