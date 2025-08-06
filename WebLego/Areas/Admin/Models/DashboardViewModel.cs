namespace WebLego.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string FilterType { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageRating { get; set; }
        public string BestSellingProduct { get; set; }
        public int BestSellingQuantity { get; set; }
        public string LowestStockProduct { get; set; }
        public int LowestStockQuantity { get; set; }
        public int ReturnRequests { get; set; }
        public List<RevenueData> RevenueByMonth { get; set; }
        public List<CategorySalesData> SalesByCategory { get; set; }
        public List<OrderStatusData> OrdersByStatus { get; set; }
    }

    public class RevenueData
    {
        public string Period { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CategorySalesData
    {
        public string CategoryName { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderStatusData
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }
}