using System.Collections.Generic;
using ThrdCtrl2.Data;

namespace ThrdCtrl2.Models
{
    public class SecurityViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public SecurityStats Stats { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<string> Modules { get; set; } = new() { "All Modules", "Inventory", "Sales", "Procurement", "Stock Adjustment", "User Management", "Auth" };
        
        public int? SelectedUserId { get; set; }
        public string? SelectedModule { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 15;
    }
}
