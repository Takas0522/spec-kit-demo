CREATE TABLE [dbo].[Tasks]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [OwnerId] UNIQUEIDENTIFIER NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'NotStarted',
    [DueDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Tasks_Users] FOREIGN KEY ([OwnerId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [CK_Task_Status] CHECK ([Status] IN ('NotStarted', 'InProgress', 'Completed')),
    CONSTRAINT [CK_Task_Title_NotEmpty] CHECK (LEN(TRIM([Title])) > 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_Tasks_Owner_IsDeleted_Status]
    ON [dbo].[Tasks]([OwnerId] ASC, [IsDeleted] ASC, [Status] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Tasks_DueDate]
    ON [dbo].[Tasks]([DueDate] ASC);
GO
