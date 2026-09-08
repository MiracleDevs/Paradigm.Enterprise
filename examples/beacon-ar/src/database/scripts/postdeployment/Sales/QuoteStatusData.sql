MERGE [dbo].[QuoteStatus] AS [Target]
USING (VALUES
    (1, N'draft', N'Draft', 1),
    (2, N'sent', N'Sent', 1),
    (3, N'accepted', N'Accepted', 1),
    (4, N'rejected', N'Rejected', 1),
    (5, N'expired', N'Expired', 1)
) AS [Source] ([Id], [Code], [DisplayName], [IsActive])
ON [Target].[Id] = [Source].[Id]
WHEN MATCHED THEN UPDATE SET [Code] = [Source].[Code], [DisplayName] = [Source].[DisplayName], [IsActive] = [Source].[IsActive]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Code], [DisplayName], [IsActive]) VALUES ([Source].[Id], [Source].[Code], [Source].[DisplayName], [Source].[IsActive]);
