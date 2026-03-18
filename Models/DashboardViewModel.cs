using ThrdCtrl2.Models;
using System.Collections.Generic;

namespace ThrdCtrl2.Models
{
    public class DashboardViewModel
    {
        public decimal TotalInventoryValue { get; set; }
        public int LowStockItemsCount { get; set; }
        public int ActiveSalesOrdersCount { get; set; }
        public int PendingAdjustmentsCount { get; set; }
        public List<StockMovementViewModel> RecentMovements { get; set; } = new();
        
        // Redesigned dashboard additional data
        public List<ValuationHistoryPoint> ValuationTrend { get; set; } = new();
        public List<AuditLog> RecentActivity { get; set; } = new();
        public List<StockAdjustment> PendingAdjustments { get; set; } = new();

        // Health metrics (Static for now but structured)
        public string DatabaseStatus { get; set; } = "Online";
        public string ApiGatewayStatus { get; set; } = "Operational";
        public string LastBackupTime { get; set; } = "6 hours ago";
        public string LatestAuditLogTime { get; set; } = "2 mins ago";

        // Pagination for Pending Authorizations
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
    }
}
