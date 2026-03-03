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
        public string CreatedByName { get; set; }
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
    }

    public class PurchaseOrder
    {
        public int PurchaseOrderID { get; set; }
        public int CompanyID { get; set; }
        public int SupplierID { get; set; }
        public int CreatedBy { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } // Pending, Received
        public decimal TotalCost { get; set; }
        public string SupplierName { get; set; }
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
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public string Action { get; set; }
        public string Module { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string FullName { get; set; }
    }
}
