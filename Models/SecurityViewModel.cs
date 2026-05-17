using System.Collections.Generic;
using ThrdCtrl2.Data;

namespace ThrdCtrl2.Models
{
    public class SecurityViewModel
    {
        public List<AuditLog> Logs { get; set; } = new();
        public SecurityStats Stats { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Company> Companies { get; set; } = new();
        public List<string> Modules { get; set; } = new() { "All Modules", "Inventory", "Sales", "Procurement", "Stock Adjustment", "User Management", "Auth" };
        public List<string> Roles { get; set; } = new() { "All Roles", "Company Admin", "Inventory Manager", "Sales Representative", "Purchasing Officer", "User", "Auditor" };
        public List<string> Actions { get; set; } = new() { "All Actions", "Create", "Update", "Delete", "Login", "Failed Login", "Logout", "Change Password", "Lockout", "Approve", "Reject" };

        public int? SelectedUserId { get; set; }
        public int? SelectedCompanyId { get; set; }
        public string? SelectedModule { get; set; }
        public string? SelectedRole { get; set; }
        public string? SelectedAction { get; set; }
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 15;
    }
}
