CREATE VIEW [dbo].[QuotePricingView]
WITH SCHEMABINDING
AS
    SELECT
        [Quote].[Id] AS [QuoteId],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([QuoteLine].[LineSubtotal]), 0)) AS [Subtotal],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([QuoteLine].[DiscountAmount]), 0)) AS [DiscountTotal],
        CONVERT(DECIMAL(19,2), ISNULL(SUM([QuoteLine].[LineTotal]), 0)) AS [GrandTotal]
    FROM [dbo].[Quote] AS [Quote]
    LEFT JOIN [dbo].[QuoteLine] AS [QuoteLine] ON [QuoteLine].[QuoteId] = [Quote].[Id]
    GROUP BY [Quote].[Id];
