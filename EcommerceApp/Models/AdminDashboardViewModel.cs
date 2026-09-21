namespace EcommerceApp.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalStock { get; set; }
        public decimal InventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int TotalUsers { get; set; }
        public int PendingCustomOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PendingSerigrafiado { get; set; }
        public List<Product> RecentProducts { get; set; } = new();

        public Dictionary<string, int> GeneroCounts { get; set; } = new();
        public Dictionary<string, int> TallaCounts { get; set; } = new();
    }
}
