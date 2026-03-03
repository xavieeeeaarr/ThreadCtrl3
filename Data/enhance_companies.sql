-- Add Status and UserCount to Companies table
IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('Companies') AND name = 'Status')
BEGIN
    ALTER TABLE Companies ADD Status VARCHAR(20) NOT NULL DEFAULT 'Active';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('Companies') AND name = 'UserCount')
BEGIN
    ALTER TABLE Companies ADD UserCount INT NOT NULL DEFAULT 0;
END
GO

-- Optional: Initialize UserCount based on existing users (if any)
UPDATE c
SET c.UserCount = (SELECT COUNT(*) FROM Users u WHERE u.CompanyID = c.CompanyID)
FROM Companies c;
GO
