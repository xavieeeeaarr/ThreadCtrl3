using System;
using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    // --- INVENTORY MODULE ---

    public class Category
    {
        public int CategoryID { get; set; }
        public int CompanyID { get; set; }
        public string CategoryName { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class Product
    {
        public int ProductID { get; set; }
        public int CompanyID { get; set; }
        public int CategoryID { get; set; }
        public string ProductName { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsActive { get; set; }
        
        // Navigation-like properties for display
        public string CategoryName { get; set; } = "";
    }

    public class ProductVariant
    {
        public int VariantID { get; set; }
        public int ProductID { get; set; }
        public string Size { get; set; } = "";
        public string Color { get; set; } = "";
        public string SKU { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal Price { get; set; }
        
        // Display property
        public string ProductName { get; set; } = "";
    }

    public class Warehouse
    {
        public int WarehouseID { get; set; }
        public int CompanyID { get; set; }
        public string WarehouseName { get; set; } = "";
        public string Location { get; set; } = "";
    }

    public class InventoryItem
    {
        public int InventoryID { get; set; }
        public int CompanyID { get; set; }
        public int VariantID { get; set; }
        public int WarehouseID { get; set; }
        public int QuantityOnHand { get; set; }
        public int MinimumStockLevel { get; set; }
        public DateTime LastUpdated { get; set; }

        // Display properties
        public string ProductName { get; set; } = "";
        public string VariantInfo => $"{Color} | {Size}";
        public string Color { get; set; } = "";
        public string Size { get; set; } = "";
        public string SKU { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal VariantPrice { get; set; }
        public string WarehouseName { get; set; } = "";
    }

    public class InventoryTransaction
    {
        public int InventoryTransactionID { get; set; }
        public int CompanyID { get; set; }
        public int InventoryID { get; set; }
        public int UserID { get; set; }
        public string TransactionType { get; set; } // Sale, Purchase, Adjustment
        public int? ReferenceID { get; set; }
        public int ChangeQuantity { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime TransactionDate { get; set; }
    }

    // --- SALES MODULE ---

    public class SalesOrder
    {
        public int SalesOrderID { get; set; }
        public int CompanyID { get; set; }
        public int CreatedBy { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // Pending, Paid
        public decimal TotalAmount { get; set; }
        public int TotalQuantity { get; set; }
        public string CreatedByName { get; set; }
        public string CustomerName { get; set; } = "";
    }

    public class SalesOrderItem
    {
        public int SalesOrderItemID { get; set; }
        public int SalesOrderID { get; set; }
        public int VariantID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class Payment
    {
        public int PaymentID { get; set; }
        public int CompanyID { get; set; }
        public int SalesOrderID { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; }
    }

    // --- PROCUREMENT MODULE ---

    public class Supplier
    {
        public int SupplierID { get; set; }
        public int CompanyID { get; set; }
        public string SupplierName { get; set; }
        public string ContactInfo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class PurchaseOrder
    {
        public int PurchaseOrderID { get; set; }
        public int CompanyID { get; set; }
        public int SupplierID { get; set; }
        public int? WarehouseID { get; set; }
        public int CreatedBy { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // Pending, Received
        public decimal TotalCost { get; set; }
        public string SupplierName { get; set; }
        public string ItemSummary { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class PurchaseOrderItem
    {
        public int PurchaseOrderItemID { get; set; }
        public int PurchaseOrderID { get; set; }
        public int VariantID { get; set; }
        public int Quantity { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal Subtotal { get; set; }
    }

    // --- SHARED ---

    public class AuditLog
    {
        public int LogID { get; set; }
        public int? CompanyID { get; set; }
        public int? UserID { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = "";
        public string Module { get; set; } = "";
        public string? Details { get; set; }
        public string? IPAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
    // --- STOCK ADJUSTMENT ---

    public class StockAdjustment
    {
        public int AdjustmentID { get; set; }
        public int CompanyID { get; set; }
        public int VariantID { get; set; }
        public int WarehouseID { get; set; }
        public string Type { get; set; } = "Correction"; // Damage, Correction, Write-off
        public int ChangeQuantity { get; set; }
        public int RequestedBy { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? Reason { get; set; }
        public DateTime DateRequested { get; set; } = DateTime.Now;
        public int? ApprovedBy { get; set; }
        public DateTime? DateProcessed { get; set; }

        // Display properties
        public string ProductName { get; set; } = "";
        public string VariantInfo { get; set; } = "";
        public string RequestedByName { get; set; } = "";
        public string WarehouseName { get; set; } = "";
    }

    // --- REPORTING ---
    public class InventoryValuationSummary
    {
        public decimal TotalAssetValue { get; set; }
        public decimal AvgUnitCost { get; set; }
        public int TotalSKUs { get; set; }
        public string TopCategoryName { get; set; }
        public decimal TopCategoryValue { get; set; }
    }

    public class StockMovementViewModel
    {
        public string TransactionID { get; set; }
        public string Type { get; set; }
        public string ProductVariant { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }

    public class ValuationHistoryPoint
    {
        public string Month { get; set; }
        public decimal Value { get; set; }
    }

    public class CategoryValuePoint
    {
        public string CategoryName { get; set; }
        public decimal Value { get; set; }
        public double Percentage { get; set; }
    }

    public class DetailedValuationViewModel
    {
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public string Category { get; set; }
        public string Warehouse { get; set; }
        public int QtyOnHand { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastRevalDate { get; set; }
    }
}
