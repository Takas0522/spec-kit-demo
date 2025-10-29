CREATE TRIGGER [dbo].[TR_Task_UpdateModifiedAt]
ON [dbo].[Tasks]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Only update ModifiedAt if it hasn't been explicitly set in the UPDATE
    UPDATE [dbo].[Tasks]
    SET [ModifiedAt] = GETUTCDATE()
    FROM [dbo].[Tasks] t
    INNER JOIN inserted i ON t.[Id] = i.[Id]
    WHERE t.[ModifiedAt] = i.[ModifiedAt]; -- Avoid infinite loop
END;
GO
