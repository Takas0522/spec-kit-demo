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

-- Insert demo user if not exists
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'demo@example.com')
BEGIN
    INSERT INTO [dbo].[Users] ([EntraObjectId], [Email], [DisplayName], [IsActive])
    VALUES ('demo-object-id', 'demo@example.com', 'Demo User', 1);
END
GO
