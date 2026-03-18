using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class StockAdjustmentViewModel
    {
        public List<StockAdjustment> Adjustments { get; set; } = new();
        public List<ProductVariant> AllVariants { get; set; } = new();
        public List<Warehouse> AllWarehouses { get; set; } = new();
        
        public string ActiveTab { get; set; } = "All"; // All, Pending, Rejected
        
        // Summary stats
        public int PendingCount { get; set; }
        public int ThisMonthDamages { get; set; }
        public int NetCorrections { get; set; }

        // Filter persistence
        public string? SelectedType { get; set; }
        public string? SelectedStatus { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
