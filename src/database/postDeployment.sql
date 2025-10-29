/*
Post-Deployment Script Template                            
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.        
 Use SQLCMD syntax to include a file in the post-deployment script.            
 Example:      :r .\myfile.sql                                
 Use SQLCMD syntax to reference a variable in the post-deployment script.        
 Example:      :setvar TableName MyTable                            
               SELECT * FROM [$(TableName)]                    
--------------------------------------------------------------------------------------
*/

-- Insert demo users if not exists
DECLARE @DemoUserId1 UNIQUEIDENTIFIER = NEWID();
DECLARE @DemoUserId2 UNIQUEIDENTIFIER = NEWID();
DECLARE @DemoUserId3 UNIQUEIDENTIFIER = NEWID();

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'demo@example.com')
BEGIN
    SET @DemoUserId1 = NEWID();
    INSERT INTO [dbo].[Users] ([Id], [EntraObjectId], [Email], [DisplayName], [CreatedAt], [LastLoginAt], [IsActive])
    VALUES (@DemoUserId1, 'demo-object-id', 'demo@example.com', 'Demo User', GETUTCDATE(), NULL, 1);
    PRINT 'Created demo user: demo@example.com';
END
ELSE
BEGIN
    SELECT @DemoUserId1 = [Id] FROM [dbo].[Users] WHERE [Email] = 'demo@example.com';
    PRINT 'Demo user already exists: demo@example.com';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'user2@example.com')
BEGIN
    SET @DemoUserId2 = NEWID();
    INSERT INTO [dbo].[Users] ([Id], [EntraObjectId], [Email], [DisplayName], [CreatedAt], [LastLoginAt], [IsActive])
    VALUES (@DemoUserId2, 'user2-object-id', 'user2@example.com', 'Test User 2', GETUTCDATE(), NULL, 1);
    PRINT 'Created demo user: user2@example.com';
END
ELSE
BEGIN
    SELECT @DemoUserId2 = [Id] FROM [dbo].[Users] WHERE [Email] = 'user2@example.com';
    PRINT 'Demo user already exists: user2@example.com';
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'user3@example.com')
BEGIN
    SET @DemoUserId3 = NEWID();
    INSERT INTO [dbo].[Users] ([Id], [EntraObjectId], [Email], [DisplayName], [CreatedAt], [LastLoginAt], [IsActive])
    VALUES (@DemoUserId3, 'user3-object-id', 'user3@example.com', 'Test User 3', GETUTCDATE(), NULL, 1);
    PRINT 'Created demo user: user3@example.com';
END
ELSE
BEGIN
    SELECT @DemoUserId3 = [Id] FROM [dbo].[Users] WHERE [Email] = 'user3@example.com';
    PRINT 'Demo user already exists: user3@example.com';
END

-- Insert sample tasks for testing
DECLARE @Task1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Task2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Task3Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Task4Id UNIQUEIDENTIFIER = NEWID();

-- Task 1: NotStarted task for demo user
IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [OwnerId] = @DemoUserId1 AND [Title] = 'プロジェクト計画書を作成')
BEGIN
    SET @Task1Id = NEWID();
    INSERT INTO [dbo].[Tasks] ([Id], [OwnerId], [Title], [Description], [Status], [DueDate], [CreatedAt], [ModifiedAt], [IsDeleted])
    VALUES (
        @Task1Id,
        @DemoUserId1,
        N'プロジェクト計画書を作成',
        N'新規プロジェクトの計画書を作成し、チームメンバーと共有する',
        'NotStarted',
        DATEADD(day, 7, GETUTCDATE()),
        GETUTCDATE(),
        GETUTCDATE(),
        0
    );
    PRINT 'Created sample task: プロジェクト計画書を作成';
END

-- Task 2: InProgress task for demo user
IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [OwnerId] = @DemoUserId1 AND [Title] = 'データベース設計レビュー')
BEGIN
    SET @Task2Id = NEWID();
    INSERT INTO [dbo].[Tasks] ([Id], [OwnerId], [Title], [Description], [Status], [DueDate], [CreatedAt], [ModifiedAt], [IsDeleted])
    VALUES (
        @Task2Id,
        @DemoUserId1,
        N'データベース設計レビュー',
        N'データベーススキーマのレビューを実施し、最適化の提案を行う',
        'InProgress',
        DATEADD(day, 3, GETUTCDATE()),
        DATEADD(day, -2, GETUTCDATE()),
        GETUTCDATE(),
        0
    );
    PRINT 'Created sample task: データベース設計レビュー';
END

-- Task 3: Completed task for demo user
IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [OwnerId] = @DemoUserId1 AND [Title] = 'ドキュメント更新')
BEGIN
    SET @Task3Id = NEWID();
    INSERT INTO [dbo].[Tasks] ([Id], [OwnerId], [Title], [Description], [Status], [DueDate], [CreatedAt], [ModifiedAt], [IsDeleted])
    VALUES (
        @Task3Id,
        @DemoUserId1,
        N'ドキュメント更新',
        N'APIドキュメントを最新のバージョンに更新する',
        'Completed',
        NULL,
        DATEADD(day, -5, GETUTCDATE()),
        DATEADD(day, -1, GETUTCDATE()),
        0
    );
    PRINT 'Created sample task: ドキュメント更新';
END

-- Task 4: Task for user2 to test sharing
IF NOT EXISTS (SELECT 1 FROM [dbo].[Tasks] WHERE [OwnerId] = @DemoUserId2 AND [Title] = 'コードレビュー依頼')
BEGIN
    SET @Task4Id = NEWID();
    INSERT INTO [dbo].[Tasks] ([Id], [OwnerId], [Title], [Description], [Status], [DueDate], [CreatedAt], [ModifiedAt], [IsDeleted])
    VALUES (
        @Task4Id,
        @DemoUserId2,
        N'コードレビュー依頼',
        N'新しい機能のコードレビューを依頼する',
        'InProgress',
        DATEADD(day, 2, GETUTCDATE()),
        GETUTCDATE(),
        GETUTCDATE(),
        0
    );
    PRINT 'Created sample task: コードレビュー依頼';
END

-- Create task shares for testing
-- Share Task2 (InProgress) with User2
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskShares] WHERE [TaskId] = @Task2Id AND [SharedWithUserId] = @DemoUserId2)
BEGIN
    INSERT INTO [dbo].[TaskShares] ([Id], [TaskId], [SharedByUserId], [SharedWithUserId], [SharedAt], [CanEdit])
    VALUES (
        NEWID(),
        @Task2Id,
        @DemoUserId1,
        @DemoUserId2,
        GETUTCDATE(),
        0
    );
    PRINT 'Created task share: Task2 shared with User2';
END

-- Share Task4 (from User2) with Demo User
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskShares] WHERE [TaskId] = @Task4Id AND [SharedWithUserId] = @DemoUserId1)
BEGIN
    INSERT INTO [dbo].[TaskShares] ([Id], [TaskId], [SharedByUserId], [SharedWithUserId], [SharedAt], [CanEdit])
    VALUES (
        NEWID(),
        @Task4Id,
        @DemoUserId2,
        @DemoUserId1,
        GETUTCDATE(),
        0
    );
    PRINT 'Created task share: Task4 shared with Demo User';
END

PRINT 'Post-deployment script completed successfully';
GO
