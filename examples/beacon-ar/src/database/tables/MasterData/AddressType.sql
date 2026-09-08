CREATE TABLE [dbo].[AddressType]
(
    [Id] INT NOT NULL,
    [Code] NVARCHAR(32) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_AddressType_IsActive] DEFAULT (1),
    CONSTRAINT [PK_AddressType] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_AddressType_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_AddressType_Code_NotBlank] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0)
);
