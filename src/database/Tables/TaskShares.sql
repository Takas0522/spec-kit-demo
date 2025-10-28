CREATE TABLE [dbo].[TaskShares]
(
    [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [TaskId] UNIQUEIDENTIFIER NOT NULL,
    [SharedByUserId] UNIQUEIDENTIFIER NOT NULL,
    [SharedWithUserId] UNIQUEIDENTIFIER NOT NULL,
    [SharedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CanEdit] BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT [PK_TaskShares] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TaskShares_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskShares_SharedBy] FOREIGN KEY ([SharedByUserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [FK_TaskShares_SharedWith] FOREIGN KEY ([SharedWithUserId]) REFERENCES [dbo].[Users]([Id]),
    CONSTRAINT [CK_TaskShare_NotSelf] CHECK ([SharedByUserId] <> [SharedWithUserId])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_TaskShares_Task_SharedWith]
    ON [dbo].[TaskShares]([TaskId] ASC, [SharedWithUserId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_TaskShares_SharedWithUserId]
    ON [dbo].[TaskShares]([SharedWithUserId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_TaskShares_TaskId]
    ON [dbo].[TaskShares]([TaskId] ASC);
GO
