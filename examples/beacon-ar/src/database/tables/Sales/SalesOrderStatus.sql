CREATE TABLE [dbo].[SalesOrderStatus]
(
    [Id] INT NOT NULL,
    [Code] NVARCHAR(32) NOT NULL,
    [DisplayName] NVARCHAR(100) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_SalesOrderStatus_IsActive] DEFAULT (1),
    CONSTRAINT [PK_SalesOrderStatus] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_SalesOrderStatus_Code] UNIQUE ([Code]),
    CONSTRAINT [CK_SalesOrderStatus_Code_NotBlank] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0)
);
