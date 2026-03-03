-- =============================================
-- Script: schema.sql
-- Description: Create tables for Roles, Users, and UserSessions
-- Created: 2026-02-21
-- =============================================

-- 1. Create Roles Table
CREATE TABLE Roles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(150) NULL
);
GO

-- 2. Insert Default Roles
INSERT INTO Roles (RoleName, Description) VALUES 
('Super Admin', 'Full system access and configurations'),
('Company Admin', 'Admin of the companys'),
('Inventory Manager', 'Manage stock levels, categories, and warehouses'),
('Sales Staff', 'Process customer orders and view availability'),
('Procurement Officer', 'Handle purchase orders and supplier relations'),
('Auditor', 'View-only access for financial and stock auditing');
GO

-- 3. Create Users Table
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT NOT NULL,
    FullName VARCHAR(50) NOT NULL,
    CompanyName VARCHAR(100) NULL,
    Email VARCHAR(50) NOT NULL UNIQUE,
    Password VARCHAR(255) NOT NULL, -- Prepared for hashed passwords (e.g., BCrypt/Argon2)
    Status VARCHAR(20) NOT NULL,    -- Active / Inactive
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
    -- Note: ON DELETE CASCADE is typically NOT applied here to prevent accidental mass user deletion if a role is removed.
);
GO

-- 4. Create UserSessions Table (with ON DELETE CASCADE)
CREATE TABLE UserSessions (
    SessionID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    LoginTime DATETIME NOT NULL DEFAULT GETDATE(),
    LogoutTime DATETIME NULL,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
);
GO
