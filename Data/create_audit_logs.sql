IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        LogID INT PRIMARY KEY IDENTITY(1,1),
        CompanyID INT NULL,
        UserID INT NULL,
        Action VARCHAR(100) NOT NULL,
        Module VARCHAR(50) NOT NULL,
        Details NVARCHAR(MAX) NULL,
        IPAddress VARCHAR(50) NULL,
        Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_AuditLogs_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
        CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
    );
END
GO
