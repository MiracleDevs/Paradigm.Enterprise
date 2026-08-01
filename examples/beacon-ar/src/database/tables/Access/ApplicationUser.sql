CREATE TABLE [dbo].[ApplicationUser]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Issuer] NVARCHAR(400) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [Subject] NVARCHAR(200) COLLATE Latin1_General_100_BIN2 NOT NULL,
    [DisplayName] NVARCHAR(200) NOT NULL,
    [Email] NVARCHAR(320) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_ApplicationUser_IsActive] DEFAULT (1),
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    CONSTRAINT [PK_ApplicationUser] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_ApplicationUser_Issuer_Subject] UNIQUE ([Issuer], [Subject]),
    CONSTRAINT [CK_ApplicationUser_Issuer_NotBlank] CHECK (LEN(LTRIM(RTRIM([Issuer]))) > 0),
    CONSTRAINT [CK_ApplicationUser_Subject_NotBlank] CHECK (LEN(LTRIM(RTRIM([Subject]))) > 0),
    CONSTRAINT [CK_ApplicationUser_DisplayName_NotBlank] CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0),
    CONSTRAINT [FK_ApplicationUser_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ApplicationUser_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_ApplicationUser_Email] ON [dbo].[ApplicationUser] ([Email]) WHERE [Email] IS NOT NULL;
