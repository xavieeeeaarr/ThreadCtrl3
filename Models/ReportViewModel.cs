using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class ReportViewModel
    {
        public InventoryValuationSummary Valuation { get; set; } = new();
        public List<StockMovementViewModel> RecentMovements { get; set; } = new();
        
        public List<Warehouse> Warehouses { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Filters
        public int? SelectedWarehouseId { get; set; }
        public int? SelectedCategoryId { get; set; }

        public List<DetailedValuationViewModel> DetailedValuation { get; set; } = new();
        public List<ValuationHistoryPoint> ValuationHistory { get; set; } = new();
        public List<CategoryValuePoint> CategoryDistribution { get; set; } = new();

        // Pagination for Detailed Valuation
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
