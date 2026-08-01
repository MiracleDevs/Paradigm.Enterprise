CREATE PROCEDURE [dbo].[GetDashboardSummary]
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @AsOf DATETIMEOFFSET(7) = SYSUTCDATETIME();
        DECLARE @Products BIGINT = (SELECT COUNT_BIG(1) FROM [dbo].[Product] WITH (HOLDLOCK));
        DECLARE @Customers BIGINT = (SELECT COUNT_BIG(1) FROM [dbo].[Customer] WITH (HOLDLOCK));
        DECLARE @Carriers BIGINT = (SELECT COUNT_BIG(1) FROM [dbo].[Carrier] WITH (HOLDLOCK));
        DECLARE @OpenQuotes BIGINT =
            (SELECT COUNT_BIG(1) FROM [dbo].[Quote] WITH (HOLDLOCK)
             WHERE [DeletionDate] IS NULL AND [StatusId] IN (1, 2));
        DECLARE @ActiveOrders BIGINT =
            (SELECT COUNT_BIG(1) FROM [dbo].[SalesOrder] WITH (HOLDLOCK)
             WHERE [DeletionDate] IS NULL AND [StatusId] NOT IN (5, 6));

        SELECT @Products AS [Products], @Customers AS [Customers], @Carriers AS [Carriers],
               @OpenQuotes AS [OpenQuotes], @ActiveOrders AS [ActiveOrders], @AsOf AS [AsOf];

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
