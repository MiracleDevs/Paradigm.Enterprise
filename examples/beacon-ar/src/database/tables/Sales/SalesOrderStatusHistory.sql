CREATE TABLE [dbo].[SalesOrderStatusHistory]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [SalesOrderId] INT NOT NULL,
    [StatusId] INT NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [CreationDate] DATETIMEOFFSET(7) NOT NULL,
    CONSTRAINT [PK_SalesOrderStatusHistory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrderStatusHistory_SalesOrder] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrder] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderStatusHistory_SalesOrderStatus] FOREIGN KEY ([StatusId]) REFERENCES [dbo].[SalesOrderStatus] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SalesOrderStatusHistory_ApplicationUser] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[ApplicationUser] ([Id]) ON DELETE NO ACTION
);
GO
CREATE INDEX [IX_SalesOrderStatusHistory_SalesOrderId_CreationDate_Id] ON [dbo].[SalesOrderStatusHistory] ([SalesOrderId], [CreationDate], [Id]);
