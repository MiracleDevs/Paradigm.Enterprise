CREATE TABLE [dbo].[CustomerAddress]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [AddressTypeId] INT NOT NULL,
    [Label] NVARCHAR(120) NOT NULL,
    [Line1] NVARCHAR(200) NOT NULL,
    [Line2] NVARCHAR(200) NULL,
    [City] NVARCHAR(120) NOT NULL,
    [State] NVARCHAR(120) NULL,
    [PostalCode] NVARCHAR(32) NOT NULL,
    [Country] NCHAR(2) NOT NULL,
    [IsDefaultBilling] BIT NOT NULL CONSTRAINT [DF_CustomerAddress_IsDefaultBilling] DEFAULT (0),
    [IsDefaultShipping] BIT NOT NULL CONSTRAINT [DF_CustomerAddress_IsDefaultShipping] DEFAULT (0),
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_CustomerAddress] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_CustomerAddress_CustomerId_Id] UNIQUE ([CustomerId], [Id]),
    CONSTRAINT [FK_CustomerAddress_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerAddress_AddressType] FOREIGN KEY ([AddressTypeId]) REFERENCES [dbo].[AddressType] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerAddress_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerAddress_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_CustomerAddress_Label_NotBlank] CHECK (LEN(LTRIM(RTRIM([Label]))) > 0),
    CONSTRAINT [CK_CustomerAddress_Line1_NotBlank] CHECK (LEN(LTRIM(RTRIM([Line1]))) > 0),
    CONSTRAINT [CK_CustomerAddress_City_NotBlank] CHECK (LEN(LTRIM(RTRIM([City]))) > 0),
    CONSTRAINT [CK_CustomerAddress_PostalCode_NotBlank] CHECK (LEN(LTRIM(RTRIM([PostalCode]))) > 0),
    CONSTRAINT [CK_CustomerAddress_Country] CHECK ([Country] LIKE N'[A-Z][A-Z]'),
    CONSTRAINT [CK_CustomerAddress_DefaultBillingType] CHECK ([IsDefaultBilling] = 0 OR [AddressTypeId] IN (1, 3)),
    CONSTRAINT [CK_CustomerAddress_DefaultShippingType] CHECK ([IsDefaultShipping] = 0 OR [AddressTypeId] IN (2, 3))
);
GO
CREATE UNIQUE INDEX [UQ_CustomerAddress_CustomerId_DefaultBilling] ON [dbo].[CustomerAddress] ([CustomerId]) WHERE [IsDefaultBilling] = 1;
GO
CREATE UNIQUE INDEX [UQ_CustomerAddress_CustomerId_DefaultShipping] ON [dbo].[CustomerAddress] ([CustomerId]) WHERE [IsDefaultShipping] = 1;
GO
CREATE INDEX [IX_CustomerAddress_CustomerId_AddressTypeId_Id] ON [dbo].[CustomerAddress] ([CustomerId], [AddressTypeId], [Id]);
