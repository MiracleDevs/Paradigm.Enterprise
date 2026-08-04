CREATE PROCEDURE [dbo].[AllocateSalesOrderNumber]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NEXT VALUE FOR [dbo].[SalesOrderNumberSequence];
END;
