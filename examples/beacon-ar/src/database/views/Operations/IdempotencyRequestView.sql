CREATE VIEW [dbo].[IdempotencyRequestView]
WITH SCHEMABINDING
AS
    SELECT
        [Request].[Id],
        [Request].[UserId],
        [User].[DisplayName] AS [UserDisplayName],
        [Request].[Operation],
        [Request].[KeyHash],
        [Request].[RequestHash],
        [Request].[StateId],
        [State].[Code] AS [StateCode],
        [State].[DisplayName] AS [StateDisplayName],
        [Request].[ResourceType],
        [Request].[ResourceId],
        [Request].[ResponseStatusCode],
        [Request].[CreatedByUserId],
        [CreatedByUser].[DisplayName] AS [CreatedByUserDisplayName],
        [Request].[CreationDate],
        [Request].[ModifiedByUserId],
        [ModifiedByUser].[DisplayName] AS [ModifiedByUserDisplayName],
        [Request].[ModificationDate],
        [Request].[CompletionDate],
        [Request].[ExpirationDate]
    FROM [dbo].[IdempotencyRequest] AS [Request]
    INNER JOIN [dbo].[ApplicationUser] AS [User] ON [User].[Id] = [Request].[UserId]
    INNER JOIN [dbo].[IdempotencyState] AS [State] ON [State].[Id] = [Request].[StateId]
    LEFT JOIN [dbo].[ApplicationUser] AS [CreatedByUser] ON [CreatedByUser].[Id] = [Request].[CreatedByUserId]
    LEFT JOIN [dbo].[ApplicationUser] AS [ModifiedByUser] ON [ModifiedByUser].[Id] = [Request].[ModifiedByUserId];
