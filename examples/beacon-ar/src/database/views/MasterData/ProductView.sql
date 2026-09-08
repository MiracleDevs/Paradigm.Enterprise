CREATE VIEW [dbo].[ProductView]
WITH SCHEMABINDING
AS
    SELECT
        [Product].[Id],
        [Product].[Sku],
        [Product].[Name],
        [Product].[Category],
        [Product].[UnitPrice],
        [Product].[StockQuantity],
        [Product].[ThumbnailUrl],
        [Product].[IsActive],
        [Product].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Product].[CreationDate],
        [Product].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Product].[ModificationDate],
        [Product].[RowVersion]
    FROM [dbo].[Product] AS [Product]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Product].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Product].[ModifiedByUserId];
