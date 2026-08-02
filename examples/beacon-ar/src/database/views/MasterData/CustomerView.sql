CREATE VIEW [dbo].[CustomerView]
WITH SCHEMABINDING
AS
    SELECT
        [Customer].[Id],
        [Customer].[AccountNumber],
        [Customer].[Name],
        [Customer].[Email],
        [Customer].[Phone],
        [Customer].[CreditLimit],
        [Customer].[PaymentTermsDays],
        [Customer].[IsActive],
        [Customer].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Customer].[CreationDate],
        [Customer].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Customer].[ModificationDate],
        [Customer].[RowVersion]
    FROM [dbo].[Customer] AS [Customer]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Customer].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Customer].[ModifiedByUserId];
