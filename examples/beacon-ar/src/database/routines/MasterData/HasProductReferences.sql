CREATE PROCEDURE [dbo].[HasProductReferences]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS (SELECT 1 FROM [dbo].[QuoteLine] WHERE [ProductId] = @Id)
        OR EXISTS (SELECT 1 FROM [dbo].[SalesOrderLine] WHERE [ProductId] = @Id) THEN 1 ELSE 0 END);
END;
