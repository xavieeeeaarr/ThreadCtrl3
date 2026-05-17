-- Create UserOTPs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserOTPs')
BEGIN
    CREATE TABLE UserOTPs (
        OTPID INT PRIMARY KEY IDENTITY(1,1),
        UserID INT NOT NULL,
        OTPCode VARCHAR(10) NOT NULL,
        Type VARCHAR(20) NOT NULL, -- Login, Register, Reset
        ExpiryTime DATETIME NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_UserOTPs_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
    );
END
GO

-- Create PasswordResetTokens Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
BEGIN
    CREATE TABLE PasswordResetTokens (
        TokenID INT PRIMARY KEY IDENTITY(1,1),
        UserID INT NOT NULL,
        Token VARCHAR(100) NOT NULL,
        ExpiryTime DATETIME NOT NULL,
        IsUsed BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ResetTokens_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
    );
END
GO
