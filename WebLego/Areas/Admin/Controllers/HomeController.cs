using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class HomeController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public HomeController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string filterType = "")
        {
            // Lấy thông tin người dùng hiện tại
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized("Không tìm thấy người dùng.");

            var roleId = currentUser.RoleId;

            // Kiểm tra phân quyền: Chỉ Quản lý (RoleId = 3) hoặc Nhân viên bán hàng (RoleId = 2) được truy cập
            if (roleId != 3 && roleId != 2)
                return Forbid("Bạn không có quyền truy cập trang này.");

            // Gán RoleId vào ViewBag để sử dụng trong View nếu cần
            ViewBag.CurrentRoleId = roleId;

            // Lấy năm hiện tại
            var currentYear = DateTime.Now.Year;

            // Kiểm tra nếu có filterType (bộ lọc nhanh)
            if (!string.IsNullOrEmpty(filterType) && filterType != "")
            {
                switch (filterType)
                {
                    case "Week":
                        startDate = DateTime.Now.AddDays(-7);
                        endDate = DateTime.Now;
                        break;
                    case "Month":
                        startDate = new DateTime(currentYear, DateTime.Now.Month, 1);
                        endDate = startDate.Value.AddMonths(1).AddDays(-1);
                        break;
                    case "Quarter1":
                        startDate = new DateTime(currentYear, 1, 1);
                        endDate = new DateTime(currentYear, 3, 31);
                        break;
                    case "Quarter2":
                        startDate = new DateTime(currentYear, 4, 1);
                        endDate = new DateTime(currentYear, 6, 30);
                        break;
                    case "Quarter3":
                        startDate = new DateTime(currentYear, 7, 1);
                        endDate = new DateTime(currentYear, 9, 30);
                        break;
                    case "Quarter4":
                        startDate = new DateTime(currentYear, 10, 1);
                        endDate = new DateTime(currentYear, 12, 31);
                        break;
                    case "Year":
                        startDate = new DateTime(currentYear, 1, 1);
                        endDate = new DateTime(currentYear, 12, 31);
                        break;
                    default:
                        // Nếu filterType không hợp lệ, đặt về mặc định (toàn bộ năm hiện tại)
                        startDate = new DateTime(currentYear, 1, 1);
                        endDate = new DateTime(currentYear, 12, 31);
                        filterType = "Year";
                        break;
                }
            }
            else
            {
                // Nếu không có filterType, sử dụng custom dates hoặc mặc định
                startDate ??= new DateTime(currentYear, 1, 1);
                endDate ??= DateTime.Now;
                filterType = "";
            }

            // Đảm bảo endDate bao gồm cả ngày
            endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

            // Validate dates
            if (startDate > endDate)
            {
                ModelState.AddModelError("", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                return View(new DashboardViewModel
                {
                    StartDate = DateTime.Now.Date.AddDays(-7),
                    EndDate = DateTime.Now.Date,
                    FilterType = ""
                });
            }

            // Calculate statistics
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalAmount ?? 0);
            var totalOrders = orders.Count;
            var averageRating = await _context.ProductReviews
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .AverageAsync(r => (decimal?)r.Rating) ?? 0;

            var bestSellingProduct = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                .GroupBy(od => od.Product)
                .OrderByDescending(g => g.Sum(od => od.Quantity))
                .Select(g => new { Product = g.Key, Quantity = g.Sum(od => od.Quantity) })
                .FirstOrDefaultAsync();

            var lowestStockProduct = await _context.Products
                .Where(p => p.ProductStatus == "Hoạt động")
                .OrderBy(p => p.StockQuantity)
                .FirstOrDefaultAsync();

            var returnRequests = await _context.ProductReturns
                .Where(r => r.RequestedAt >= startDate && r.RequestedAt <= endDate)
                .CountAsync();

            // Revenue by time (for bar chart)
            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => new { o.OrderDate.Value.Year, o.OrderDate.Value.Month })
                .Select(g => new RevenueData
                {
                    Period = $"{g.Key.Year}-{g.Key.Month:00}",
                    Revenue = g.Sum(o => o.TotalAmount ?? 0)
                })
                .ToListAsync();

            // Sales by category (for pie chart)
            var salesByCategory = await _context.OrderDetails
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                .GroupBy(od => od.Product.Category)
                .Select(g => new CategorySalesData
                {
                    CategoryName = g.Key.CategoryName,
                    Quantity = g.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // Orders by status (for bar chart)
            var ordersByStatus = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderStatus)
                .Select(g => new OrderStatusData
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                StartDate = startDate.Value.Date, // Ensure date-only for display
                EndDate = endDate.Value.Date,    // Ensure date-only for display
                FilterType = filterType,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageRating = averageRating,
                BestSellingProduct = bestSellingProduct?.Product.ProductName,
                BestSellingQuantity = bestSellingProduct?.Quantity ?? 0,
                LowestStockProduct = lowestStockProduct?.ProductName,
                LowestStockQuantity = lowestStockProduct?.StockQuantity ?? 0,
                ReturnRequests = returnRequests,
                RevenueByMonth = revenueByMonth,
                SalesByCategory = salesByCategory,
                OrdersByStatus = ordersByStatus
            };

            return View(viewModel);
        }
    }
}