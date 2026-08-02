CREATE VIEW [dbo].[ApplicationUserView]
WITH SCHEMABINDING
AS
    SELECT
        [User].[Id],
        [User].[Issuer],
        [User].[Subject],
        [User].[DisplayName],
        [User].[Email],
        [User].[IsActive],
        [User].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [User].[CreationDate],
        [User].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [User].[ModificationDate]
    FROM [dbo].[ApplicationUser] AS [User]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [User].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [User].[ModifiedByUserId];
