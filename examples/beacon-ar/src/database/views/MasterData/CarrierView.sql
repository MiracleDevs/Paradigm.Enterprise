CREATE VIEW [dbo].[CarrierView]
WITH SCHEMABINDING
AS
    SELECT
        [Carrier].[Id],
        [Carrier].[Code],
        [Carrier].[Name],
        [Carrier].[ServiceLevel],
        [Carrier].[TrackingUrlTemplate],
        [Carrier].[IsActive],
        [Carrier].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Carrier].[CreationDate],
        [Carrier].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Carrier].[ModificationDate],
        [Carrier].[RowVersion]
    FROM [dbo].[Carrier] AS [Carrier]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Carrier].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Carrier].[ModifiedByUserId];
