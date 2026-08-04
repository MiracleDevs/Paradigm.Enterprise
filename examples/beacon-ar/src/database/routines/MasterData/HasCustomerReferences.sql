CREATE PROCEDURE [dbo].[HasCustomerReferences]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS (SELECT 1 FROM [dbo].[CustomerAddress] WHERE [CustomerId] = @Id)
        OR EXISTS (SELECT 1 FROM [dbo].[Quote] WHERE [CustomerId] = @Id)
        OR EXISTS (SELECT 1 FROM [dbo].[SalesOrder] WHERE [CustomerId] = @Id) THEN 1 ELSE 0 END);
END;
