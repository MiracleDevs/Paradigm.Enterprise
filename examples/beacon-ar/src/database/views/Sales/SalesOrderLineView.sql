CREATE VIEW [dbo].[SalesOrderLineView]
WITH SCHEMABINDING
AS
    SELECT
        [Line].[Id],
        [Line].[SalesOrderId],
        [SalesOrder].[OrderNumber],
        [Line].[ProductId],
        [Product].[Sku] AS [ProductSku],
        [Product].[Name] AS [ProductName],
        [Product].[IsActive] AS [ProductIsActive],
        [Line].[SkuSnapshot],
        [Line].[ProductNameSnapshot],
        [Line].[Quantity],
        [Line].[UnitPrice],
        [Line].[DiscountPercent],
        [Line].[LineSubtotal],
        [Line].[DiscountAmount],
        [Line].[LineTotal]
    FROM [dbo].[SalesOrderLine] AS [Line]
    INNER JOIN [dbo].[SalesOrder] AS [SalesOrder] ON [SalesOrder].[Id] = [Line].[SalesOrderId]
    INNER JOIN [dbo].[Product] AS [Product] ON [Product].[Id] = [Line].[ProductId];
