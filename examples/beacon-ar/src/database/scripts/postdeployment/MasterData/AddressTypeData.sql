MERGE [dbo].[AddressType] AS [Target]
USING (VALUES
    (1, N'billing', N'Billing', 1),
    (2, N'shipping', N'Shipping', 1),
    (3, N'both', N'Billing and shipping', 1)
) AS [Source] ([Id], [Code], [DisplayName], [IsActive])
ON [Target].[Id] = [Source].[Id]
WHEN MATCHED THEN UPDATE SET [Code] = [Source].[Code], [DisplayName] = [Source].[DisplayName], [IsActive] = [Source].[IsActive]
WHEN NOT MATCHED BY TARGET THEN INSERT ([Id], [Code], [DisplayName], [IsActive]) VALUES ([Source].[Id], [Source].[Code], [Source].[DisplayName], [Source].[IsActive]);
