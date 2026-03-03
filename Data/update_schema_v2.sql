-- Add Company Admin role to the database
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Company Admin')
BEGIN
    INSERT INTO Roles (RoleName, Description)
    VALUES ('Company Admin', 'Admin of the companys');
END
GO

-- Update Users table to include CompanyName
IF NOT EXISTS (SELECT 1 FROM sys.columns 
               WHERE object_id = OBJECT_ID('Users') AND name = 'CompanyName')
BEGIN
    ALTER TABLE Users ADD CompanyName VARCHAR(100) NULL;
END
GO
