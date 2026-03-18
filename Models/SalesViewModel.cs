using System.Collections.Generic;
using System;

namespace ThrdCtrl2.Models
{
    public class SalesViewModel
    {
        public List<SalesOrder> Orders { get; set; } = new();
        
        // Filter persistence
        public string? SelectedStatus { get; set; }
        public DateTime? SelectedDate { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
