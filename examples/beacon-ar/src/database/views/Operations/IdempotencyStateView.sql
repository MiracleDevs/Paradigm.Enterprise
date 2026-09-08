CREATE VIEW [dbo].[IdempotencyStateView]
WITH SCHEMABINDING
AS
    SELECT
        [State].[Id],
        [State].[Code],
        [State].[DisplayName],
        [State].[IsActive]
    FROM [dbo].[IdempotencyState] AS [State];
