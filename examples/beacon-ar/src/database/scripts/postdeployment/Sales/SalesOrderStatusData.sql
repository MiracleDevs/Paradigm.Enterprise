MERGE [dbo].[SalesOrderStatus] AS [Target]
USING (VALUES
    (1, N'draft', N'Draft', 1),
    (2, N'confirmed', N'Confirmed', 1),
    (3, N'processing', N'Processing', 1),
    (4, N'shipped', N'Shipped', 1),
    (5, N'completed', N'Completed', 1),
    (6, N'cancelled', N'Cancelled', 1)
) AS [Source] ([Id], [Code], [DisplayName], [IsActive])
ON [Target].[Id] = [Source].[Id]
WHEN MATCHED THEN UPDATE SET [Code] = [Source].[Code], [DisplayName] = [Source].[DisplayName], [IsActive] = [Source].[IsActive]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Code], [DisplayName], [IsActive]) VALUES ([Source].[Id], [Source].[Code], [Source].[DisplayName], [Source].[IsActive]);
