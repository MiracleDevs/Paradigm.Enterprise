CREATE TABLE [dbo].[Product]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Sku] NVARCHAR(32) COLLATE Latin1_General_100_CI_AS_SC NOT NULL,
    [Name] NVARCHAR(120) NOT NULL,
    [Category] NVARCHAR(120) NOT NULL,
    [UnitPrice] DECIMAL(19,4) NOT NULL,
    [StockQuantity] INT NOT NULL,
    [ThumbnailUrl] NVARCHAR(2048) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Product_IsActive] DEFAULT (1),
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Product_Sku] UNIQUE ([Sku]),
    CONSTRAINT [FK_Product_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Product_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Product_Sku_NotBlank] CHECK (LEN(LTRIM(RTRIM([Sku]))) > 0),
    CONSTRAINT [CK_Product_Name_NotBlank] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Product_Category_NotBlank] CHECK (LEN(LTRIM(RTRIM([Category]))) > 0),
    CONSTRAINT [CK_Product_UnitPrice] CHECK ([UnitPrice] > 0),
    CONSTRAINT [CK_Product_StockQuantity] CHECK ([StockQuantity] >= 0)
);
GO
CREATE INDEX [IX_Product_IsActive_Id] ON [dbo].[Product] ([IsActive], [Id]);
GO
CREATE INDEX [IX_Product_Name_Id] ON [dbo].[Product] ([Name], [Id]);
