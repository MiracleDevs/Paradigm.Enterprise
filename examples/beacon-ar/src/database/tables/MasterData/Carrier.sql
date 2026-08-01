CREATE TABLE [dbo].[Carrier]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Code] NVARCHAR(32) COLLATE Latin1_General_100_CI_AS_SC NOT NULL,
    [Name] NVARCHAR(120) NOT NULL,
    [ServiceLevel] NVARCHAR(120) NOT NULL,
    [TrackingUrlTemplate] NVARCHAR(2048) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_Carrier_IsActive] DEFAULT (1),
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [PK_Carrier] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_Carrier_Code] UNIQUE ([Code]),
    CONSTRAINT [FK_Carrier_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Carrier_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_Carrier_Code_NotBlank] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
    CONSTRAINT [CK_Carrier_Name_NotBlank] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Carrier_ServiceLevel_NotBlank] CHECK (LEN(LTRIM(RTRIM([ServiceLevel]))) > 0)
);
GO
CREATE INDEX [IX_Carrier_IsActive_Id] ON [dbo].[Carrier] ([IsActive], [Id]);
GO
CREATE INDEX [IX_Carrier_Name_Id] ON [dbo].[Carrier] ([Name], [Id]);
