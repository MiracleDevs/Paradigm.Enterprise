CREATE TABLE [dbo].[IdempotencyRequest]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [UserId] INT NOT NULL,
    [Operation] NVARCHAR(150) NOT NULL,
    [KeyHash] BINARY(32) NOT NULL,
    [RequestHash] BINARY(32) NOT NULL,
    [StateId] INT NOT NULL,
    [ResourceType] NVARCHAR(100) NULL,
    [ResourceId] NVARCHAR(50) NULL,
    [ResponseStatusCode] SMALLINT NULL,
    [CreatedByUserId] INT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    [ModifiedByUserId] INT NULL,
    [ModificationDate] DATETIMEOFFSET(7) NULL,
    [CompletionDate] DATETIMEOFFSET(7) NULL,
    [ExpirationDate] DATETIMEOFFSET(7) NOT NULL,
    CONSTRAINT [PK_IdempotencyRequest] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_IdempotencyRequest_UserId_Operation_KeyHash] UNIQUE ([UserId], [Operation], [KeyHash]),
    CONSTRAINT [FK_IdempotencyRequest_ApplicationUser] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_IdempotencyRequest_ApplicationUser_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_IdempotencyRequest_ApplicationUser_ModifiedByUserId] FOREIGN KEY ([ModifiedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_IdempotencyRequest_IdempotencyState] FOREIGN KEY ([StateId]) REFERENCES [dbo].[IdempotencyState] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_IdempotencyRequest_Operation_NotBlank] CHECK (LEN(LTRIM(RTRIM([Operation]))) > 0),
    CONSTRAINT [CK_IdempotencyRequest_ExpirationDate] CHECK ([ExpirationDate] > [CreationDate]),
    CONSTRAINT [CK_IdempotencyRequest_CompletionDate] CHECK ([CompletionDate] IS NULL OR [CompletionDate] >= [CreationDate]),
    CONSTRAINT [CK_IdempotencyRequest_ResponseStatusCode] CHECK ([ResponseStatusCode] IS NULL OR [ResponseStatusCode] BETWEEN 100 AND 599)
);
GO
CREATE INDEX [IX_IdempotencyRequest_StateId_ExpirationDate] ON [dbo].[IdempotencyRequest] ([StateId], [ExpirationDate]);
