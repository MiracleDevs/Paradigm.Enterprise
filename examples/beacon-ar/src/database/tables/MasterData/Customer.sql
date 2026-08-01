CREATE TABLE [dbo].[Customer]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [AccountNumber] NVARCHAR(50) COLLATE Latin1_General_100_CI_AS_SC NOT NULL,
    [Name] NVARCHAR(120) NOT NULL,
    [Email] NVARCHAR(320) NOT NULL,
    [Phone] NVARCHAR(50) NULL,
    [CreditLimit] DECIMAL(19,2) NOT NULL,
    [PaymentTermsDays] SMALLINT NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Customer_IsActive] DEFAULT (1),
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_Customer] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Customer_AccountNumber] UNIQUE ([AccountNumber]),
    CONSTRAINT [FK_Customer_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Customer_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Customer_AccountNumber_NotBlank] CHECK (LEN(LTRIM(RTRIM([AccountNumber]))) > 0),
    CONSTRAINT [CK_Customer_Name_NotBlank] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Customer_Email_NotBlank] CHECK (LEN(LTRIM(RTRIM([Email]))) > 0),
    CONSTRAINT [CK_Customer_CreditLimit] CHECK ([CreditLimit] >= 0),
    CONSTRAINT [CK_Customer_PaymentTermsDays] CHECK ([PaymentTermsDays] IN (0, 15, 30, 45, 60))
);
GO
CREATE INDEX [IX_Customer_IsActive_Id] ON [dbo].[Customer] ([IsActive], [Id]);
GO
CREATE INDEX [IX_Customer_Name_Id] ON [dbo].[Customer] ([Name], [Id]);
