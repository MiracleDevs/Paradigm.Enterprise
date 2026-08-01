CREATE TABLE [dbo].[QuoteStatus]
(
    [Id] INT NOT NULL,
    [Code] NVARCHAR(32) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_QuoteStatus_IsActive] DEFAULT (1),
    CONSTRAINT [PK_QuoteStatus] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_QuoteStatus_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_QuoteStatus_Code_NotBlank] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0)
);
