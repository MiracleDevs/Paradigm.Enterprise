MERGE [dbo].[IdempotencyState] AS [Target]
USING (VALUES
    (1, N'in_progress', N'In progress', 1),
    (2, N'completed', N'Completed', 1),
    (3, N'failed', N'Failed', 1)
) AS [Source] ([Id], [Code], [DisplayName], [IsActive])
ON [Target].[Id] = [Source].[Id]
WHEN MATCHED THEN UPDATE SET [Code] = [Source].[Code], [DisplayName] = [Source].[DisplayName], [IsActive] = [Source].[IsActive]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Code], [DisplayName], [IsActive]) VALUES ([Source].[Id], [Source].[Code], [Source].[DisplayName], [Source].[IsActive]);
