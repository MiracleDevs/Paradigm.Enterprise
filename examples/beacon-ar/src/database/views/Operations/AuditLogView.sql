CREATE VIEW [dbo].[AuditLogView]
WITH SCHEMABINDING
AS
    SELECT
        [AuditLog].[Id],
        [AuditLog].[ResourceType],
        [AuditLog].[ResourceId],
        [AuditLog].[Action],
        [AuditLog].[UserId],
        [User].[DisplayName] AS [UserDisplayName],
        [AuditLog].[RecordedAt],
        [AuditLog].[CorrelationId],
        [AuditLog].[PreviousStatusCode],
        [AuditLog].[NewStatusCode],
        [AuditLog].[MetadataJson]
    FROM [dbo].[AuditLog] AS [AuditLog]
    INNER JOIN [dbo].[ApplicationUser] AS [User] ON [User].[Id] = [AuditLog].[UserId];
