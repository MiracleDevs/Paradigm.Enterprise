CREATE TABLE [dbo].[QuoteStatusHistory]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [QuoteId] INT NOT NULL,
    [StatusId] INT NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    CONSTRAINT [PK_QuoteStatusHistory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuoteStatusHistory_Quote] FOREIGN KEY ([QuoteId]) REFERENCES [dbo].[Quote] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuoteStatusHistory_QuoteStatus] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[QuoteStatus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_QuoteStatusHistory_ApplicationUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_QuoteStatusHistory_QuoteId_CreationDate_Id] ON [dbo].[QuoteStatusHistory] ([QuoteId], [CreationDate], [Id]);
