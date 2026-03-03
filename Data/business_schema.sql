-- =============================================
-- Script: business_schema.sql
-- Description: Multi-tenant Clothing Line Inventory & Sales System
-- Enforces: CompanyID isolation, Referential Integrity, Performance Indexing
-- =============================================

-- 1. Category Table
CREATE TABLE Category (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    Description VARCHAR(150) NULL,
    CONSTRAINT FK_Category_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID)
);
CREATE INDEX IX_Category_Company ON Category(CompanyID);
GO

-- 2. Product Table
CREATE TABLE Product (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    CategoryID INT NOT NULL,
    ProductName VARCHAR(100) NOT NULL,
    Description VARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Product_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID)
);
CREATE INDEX IX_Product_Company ON Product(CompanyID);
CREATE INDEX IX_Product_Category ON Product(CategoryID);
GO

-- 3. ProductVariant Table
CREATE TABLE ProductVariant (
    VariantID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT NOT NULL,
    Size VARCHAR(10) NOT NULL,
    Color VARCHAR(30) NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    Barcode VARCHAR(50) NULL,
    CONSTRAINT FK_Variant_Product FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE CASCADE
);
CREATE INDEX IX_Variant_Product ON ProductVariant(ProductID);
GO

-- 4. Warehouse Table
CREATE TABLE Warehouse (
    WarehouseID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    WarehouseName VARCHAR(50) NOT NULL,
    Location VARCHAR(100) NULL,
    CONSTRAINT FK_Warehouse_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID)
);
CREATE INDEX IX_Warehouse_Company ON Warehouse(CompanyID);
GO

-- 5. Inventory Table
CREATE TABLE Inventory (
    InventoryID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    VariantID INT NOT NULL,
    WarehouseID INT NOT NULL,
    QuantityOnHand INT NOT NULL DEFAULT 0,
    MinimumStockLevel INT NOT NULL DEFAULT 0,
    LastUpdated DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Inventory_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_Inventory_Variant FOREIGN KEY (VariantID) REFERENCES ProductVariant(VariantID),
    CONSTRAINT FK_Inventory_Warehouse FOREIGN KEY (WarehouseID) REFERENCES Warehouse(WarehouseID)
);
CREATE INDEX IX_Inventory_Company ON Inventory(CompanyID);
CREATE INDEX IX_Inventory_Variant ON Inventory(VariantID);
CREATE INDEX IX_Inventory_Warehouse ON Inventory(WarehouseID);
GO

-- 6. AuditLog Table
CREATE TABLE AuditLog (
    LogID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    UserID INT NOT NULL,
    Action VARCHAR(100) NOT NULL,
    Module VARCHAR(50) NOT NULL,
    OldValue VARCHAR(MAX) NULL,
    NewValue VARCHAR(MAX) NULL,
    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_AuditLog_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
CREATE INDEX IX_AuditLog_Company ON AuditLog(CompanyID);
CREATE INDEX IX_AuditLog_User ON AuditLog(UserID);
GO

-- 7. Supplier Table
CREATE TABLE Supplier (
    SupplierID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    SupplierName VARCHAR(100) NOT NULL,
    ContactInfo VARCHAR(150) NULL,
    CONSTRAINT FK_Supplier_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID)
);
CREATE INDEX IX_Supplier_Company ON Supplier(CompanyID);
GO

-- 8. PurchaseOrder Table
CREATE TABLE PurchaseOrder (
    PurchaseOrderID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    SupplierID INT NOT NULL,
    CreatedBy INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Received
    TotalCost DECIMAL(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_PO_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_PO_Supplier FOREIGN KEY (SupplierID) REFERENCES Supplier(SupplierID),
    CONSTRAINT FK_PO_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
);
CREATE INDEX IX_PO_Company ON PurchaseOrder(CompanyID);
CREATE INDEX IX_PO_Supplier ON PurchaseOrder(SupplierID);
GO

-- 9. PurchaseOrderItem Table
CREATE TABLE PurchaseOrderItem (
    PurchaseOrderItemID INT PRIMARY KEY IDENTITY(1,1),
    PurchaseOrderID INT NOT NULL,
    VariantID INT NOT NULL,
    Quantity INT NOT NULL,
    CostPerUnit DECIMAL(10,2) NOT NULL,
    Subtotal AS (Quantity * CostPerUnit),
    CONSTRAINT FK_POI_Order FOREIGN KEY (PurchaseOrderID) REFERENCES PurchaseOrder(PurchaseOrderID) ON DELETE CASCADE,
    CONSTRAINT FK_POI_Variant FOREIGN KEY (VariantID) REFERENCES ProductVariant(VariantID)
);
CREATE INDEX IX_POI_Order ON PurchaseOrderItem(PurchaseOrderID);
GO

-- 10. SalesOrder Table
CREATE TABLE SalesOrder (
    SalesOrderID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    CreatedBy INT NOT NULL,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Paid
    TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_SO_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_SO_User FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
);
CREATE INDEX IX_SO_Company ON SalesOrder(CompanyID);
CREATE INDEX IX_SO_User ON SalesOrder(CreatedBy);
GO

-- 11. SalesOrderItem Table
CREATE TABLE SalesOrderItem (
    SalesOrderItemID INT PRIMARY KEY IDENTITY(1,1),
    SalesOrderID INT NOT NULL,
    VariantID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Subtotal AS (Quantity * UnitPrice),
    CONSTRAINT FK_SOI_Order FOREIGN KEY (SalesOrderID) REFERENCES SalesOrder(SalesOrderID) ON DELETE CASCADE,
    CONSTRAINT FK_SOI_Variant FOREIGN KEY (VariantID) REFERENCES ProductVariant(VariantID)
);
CREATE INDEX IX_SOI_Order ON SalesOrderItem(SalesOrderID);
GO

-- 12. Payment Table
CREATE TABLE Payment (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    SalesOrderID INT NOT NULL,
    PaymentMethod VARCHAR(30) NOT NULL, -- Cash / E-wallet
    Amount DECIMAL(10,2) NOT NULL,
    PaymentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Success', -- Success / Pending
    CONSTRAINT FK_Payment_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_Payment_Order FOREIGN KEY (SalesOrderID) REFERENCES SalesOrder(SalesOrderID)
);
CREATE INDEX IX_Payment_Company ON Payment(CompanyID);
CREATE INDEX IX_Payment_Order ON Payment(SalesOrderID);
GO

-- 13. InventoryTransaction Table
CREATE TABLE InventoryTransaction (
    InventoryTransactionID INT PRIMARY KEY IDENTITY(1,1),
    CompanyID INT NOT NULL,
    InventoryID INT NOT NULL,
    UserID INT NOT NULL,
    TransactionType VARCHAR(30) NOT NULL, -- Sale / Purchase / Adjustment
    ReferenceID INT NULL, -- PO ID or SO ID
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Transaction_Company FOREIGN KEY (CompanyID) REFERENCES Companies(CompanyID),
    CONSTRAINT FK_Transaction_Inventory FOREIGN KEY (InventoryID) REFERENCES Inventory(InventoryID),
    CONSTRAINT FK_Transaction_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
CREATE INDEX IX_Transaction_Company ON InventoryTransaction(CompanyID);
CREATE INDEX IX_Transaction_Inventory ON InventoryTransaction(InventoryID);
GO
