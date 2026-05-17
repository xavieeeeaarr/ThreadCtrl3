using System.Collections.Generic;
using ThrdCtrl2.Models;

namespace ThrdCtrl2.Models
{
    public class SuperAdminDashboardViewModel
    {
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int InactiveCompanies { get; set; }
        public int PendingCompanies { get; set; }
        public int TotalUsers { get; set; }
        public int MonthlyCount { get; set; }
        public int YearlyCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Company> RecentCompanies { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        public List<AuditLog> RecentAuditLogs { get; set; } = new();
    }

    public class CompanyManagementViewModel
    {
        public List<CompanyWithAdmin> Companies { get; set; } = new();
        public string CurrentFilter { get; set; } = "All";
        public int TotalCompanies { get; set; }
        public int ActiveCompanies { get; set; }
        public int InactiveCompanies { get; set; }
        public int PendingCompanies { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }

    public class CompanyWithAdmin : Company
    {
        public string? AdminEmail { get; set; }
    }

    public class AllUsersViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public string? SelectedRole { get; set; }
        public List<Company> Companies { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
    }
}
