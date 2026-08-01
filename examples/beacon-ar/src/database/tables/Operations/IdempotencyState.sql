CREATE TABLE [dbo].[IdempotencyState]
(
    [Id] INT NOT NULL,
    [Code] NVARCHAR(32) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_IdempotencyState_IsActive] DEFAULT (1),
    CONSTRAINT [PK_IdempotencyState] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_IdempotencyState_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_IdempotencyState_Code_NotBlank] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0)
);
