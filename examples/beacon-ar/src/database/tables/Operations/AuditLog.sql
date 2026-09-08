-- Append-only operation fact. UserId identifies the actor; rows are never modified,
-- so the mutable-entity audit quartet does not apply.
CREATE TABLE [dbo].[AuditLog]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [ResourceType] NVARCHAR(100) NOT NULL,
    [ResourceId] NVARCHAR(50) NOT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [UserId] INT NOT NULL,
    [RecordedAt] DATETIMEOFFSET(7) NOT NULL,
    [CorrelationId] NVARCHAR(128) NOT NULL,
    [PreviousStatusCode] NVARCHAR(32) NULL,
    [NewStatusCode] NVARCHAR(32) NULL,
    [MetadataJson] NVARCHAR(4000) NULL,
    CONSTRAINT [PK_AuditLog] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLog_ApplicationUser] FOREIGN KEY ([UserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [CK_AuditLog_ResourceType_NotBlank] CHECK (LEN(LTRIM(RTRIM([ResourceType]))) > 0),
    CONSTRAINT [CK_AuditLog_ResourceId_NotBlank] CHECK (LEN(LTRIM(RTRIM([ResourceId]))) > 0),
    CONSTRAINT [CK_AuditLog_Action_NotBlank] CHECK (LEN(LTRIM(RTRIM([Action]))) > 0),
    CONSTRAINT [CK_AuditLog_CorrelationId_NotBlank] CHECK (LEN(LTRIM(RTRIM([CorrelationId]))) > 0),
    CONSTRAINT [CK_AuditLog_MetadataJson_IsJson] CHECK ([MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1)
);
GO
CREATE INDEX [IX_AuditLog_ResourceType_ResourceId_RecordedAt] ON [dbo].[AuditLog] ([ResourceType], [ResourceId], [RecordedAt]);
GO
CREATE INDEX [IX_AuditLog_CorrelationId] ON [dbo].[AuditLog] ([CorrelationId]);
