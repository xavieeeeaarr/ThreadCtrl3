-- 1. Create Companies Table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Companies')
BEGIN
    CREATE TABLE Companies (
        CompanyID INT PRIMARY KEY IDENTITY(1,1),
        CompanyName VARCHAR(100) NOT NULL UNIQUE
    );
END
GO

-- 2. Update Users Table
-- Remove the old CompanyName column if it exists and add CompanyID
IF EXISTS (SELECT 1 FROM sys.columns 
           WHERE object_id = OBJECT_ID('Users') AND name = 'CompanyName')
BEGIN
    -- If there's data we want to keep, we should migrate it. 
    -- For now, let's just add CompanyID and we'll handle the migration if needed, 
    -- but since this is a new setup, we might just drop and add.
    ALTER TABLE Users DROP COLUMN CompanyName;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('Users') AND name = 'CompanyID')
BEGIN
    ALTER TABLE Users ADD CompanyID INT NULL;
    ALTER TABLE Users ADD CONSTRAINT FK_Users_Companies FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID);
END
GO
