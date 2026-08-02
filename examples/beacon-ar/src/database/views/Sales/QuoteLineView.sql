CREATE VIEW [dbo].[QuoteLineView]
WITH SCHEMABINDING
AS
    SELECT
        [Line].[Id],
        [Line].[QuoteId],
        [Quote].[QuoteNumber],
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
    FROM [dbo].[QuoteLine] AS [Line]
    INNER JOIN [dbo].[Quote] AS [Quote] ON [Quote].[Id] = [Line].[QuoteId]
    INNER JOIN [dbo].[Product] AS [Product] ON [Product].[Id] = [Line].[ProductId];
