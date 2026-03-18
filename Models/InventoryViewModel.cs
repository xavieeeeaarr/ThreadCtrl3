using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class InventoryViewModel
    {
        public List<InventoryItem> InventoryItems { get; set; }
        public List<Category> Categories { get; set; }
        public List<Warehouse> Warehouses { get; set; }
        public List<ProductVariant> ProductVariants { get; set; }
        public List<Product> Products { get; set; }
        
        // Filter persistence
        public int? SelectedCategoryId { get; set; }
        public string? SelectedStatus { get; set; }
        public int? SelectedWarehouseId { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
