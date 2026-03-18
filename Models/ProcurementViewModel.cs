using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class ProcurementViewModel
    {
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
        public List<Supplier> Suppliers { get; set; } = new();
        public List<Supplier> ActiveSuppliers { get; set; } = new();
        public List<InventoryItem> ReorderAlerts { get; set; } = new();
        
        // Summary Stats
        public int PendingApprovalCount { get; set; }
        public int ReceivingDueCount { get; set; }
        public int ActiveSuppliersCount { get; set; }

        // Filters
        public string ActiveTab { get; set; } = "PurchaseOrders";
        public string? SelectedStatus { get; set; }
        public int? SelectedSupplierId { get; set; }
        public string SelectedSupplierStatus { get; set; } = "Active";

        // For Select Lists
        public List<ProductVariant> AllVariants { get; set; } = new();
        public List<Warehouse> AllWarehouses { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
