CREATE VIEW [dbo].[SalesOrderPricingView]
WITH SCHEMABINDING
AS
    SELECT
        [SalesOrder].[Id] AS [SalesOrderId],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([SalesOrderLine].[LineSubtotal]), 0)) AS [Subtotal],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([SalesOrderLine].[DiscountAmount]), 0)) AS [DiscountTotal],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([SalesOrderLine].[LineTotal]), 0)) AS [GrandTotal]
    FROM [dbo].[SalesOrder] AS [SalesOrder]
    LEFT JOIN [dbo].[SalesOrderLine] AS [SalesOrderLine] ON [SalesOrderLine].[SalesOrderId] = [SalesOrder].[Id]
    GROUP BY [SalesOrder].[Id];
