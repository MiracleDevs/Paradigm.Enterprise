CREATE PROCEDURE [dbo].[HasAddressReferences]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS (SELECT 1 FROM [dbo].[Quote] WHERE [ShippingAddressId] = @Id)
        OR EXISTS (SELECT 1 FROM [dbo].[SalesOrder] WHERE [ShippingAddressId] = @Id) THEN 1 ELSE 0 END);
END;
