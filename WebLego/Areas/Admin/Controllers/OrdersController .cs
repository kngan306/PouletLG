using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;
using WebLego.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class OrdersController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly EmailService _emailService;
        private readonly ILogger<OrdersController> _logger;

        private static readonly List<string> AllowedStatuses = new List<string>
        {
            "Chờ thanh toán",
            "Đang xử lý",
            "Đang giao hàng",
            "Hoàn thành",
            "Đã hủy"
        };

        public OrdersController(DbpouletLgv5Context context, EmailService emailService, ILogger<OrdersController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // 1. Xem danh sách đơn hàng, hỗ trợ lọc
        public IActionResult Index(string statusFilter = null, int? month = null, int? year = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) && AllowedStatuses.Contains(statusFilter))
                query = query.Where(o => o.OrderStatus == statusFilter);
            else
                query = query.Where(o => AllowedStatuses.Contains(o.OrderStatus));

            if (month.HasValue)
                query = query.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Month == month.Value);

            if (year.HasValue)
                query = query.Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Year == year.Value);

            var orders = query.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                CustomerName = o.User.FullName,
                CustomerEmail = o.User.Email,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                ShippingFee = o.ShippingFee,
                Discount = o.Discount,
                FullAddress = $"{o.Address.SpecificAddress}, {o.Address.Ward}, {o.Address.District}, {o.Address.Province}",
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    OriginalPrice = od.Product.Price,
                    IsDiscounted = od.UnitPrice < od.Product.Price
                }).ToList()
            }).ToList();

            var user = _context.Users.FirstOrDefault(u => u.FullName == User.Identity.Name);
            ViewBag.CurrentRoleId = user?.RoleId ?? 0;

            ViewBag.StatusList = AllowedStatuses;
            return View(orders);
        }

        // 2. Cập nhật trạng thái đơn hàng
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            if (!AllowedStatuses.Contains(newStatus))
                return BadRequest("Trạng thái không hợp lệ.");

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            var roleId = currentUser.RoleId;
            var currentStatus = order.OrderStatus;

            if (roleId == 3) // Quản lý
            {
                order.OrderStatus = newStatus;
                if (newStatus == "Hoàn thành")
                    order.PaymentStatus = "Đã thanh toán";
            }
            else if (roleId == 2) // Nhân viên bán hàng
            {
                if (currentStatus == "Đang xử lý" && newStatus == "Đang giao hàng")
                {
                    order.OrderStatus = newStatus;
                }
                else
                {
                    return Forbid("Bạn chỉ được thay đổi từ 'Đang xử lý' sang 'Đang giao hàng'.");
                }
            }
            else if (roleId == 4) // Nhân viên giao hàng
            {
                if (currentStatus == "Đang giao hàng" && newStatus == "Hoàn thành")
                {
                    order.OrderStatus = newStatus;
                    order.PaymentStatus = "Đã thanh toán";
                }
                else
                {
                    return Forbid("Bạn chỉ được thay đổi từ 'Đang giao hàng' sang 'Hoàn thành'.");
                }
            }
            else
            {
                return Forbid("Bạn không có quyền thay đổi trạng thái đơn hàng.");
            }

            await _context.SaveChangesAsync();

            // Gửi email thông báo cập nhật trạng thái
            if (newStatus == "Đang giao hàng" || newStatus == "Hoàn thành")
            {
                await SendOrderStatusUpdateEmail(order, newStatus);
            }

            return Ok(new { success = true, newStatus });
        }

        // Phương thức gửi email thông báo cập nhật trạng thái đơn hàng
        private async Task SendOrderStatusUpdateEmail(Order order, string newStatus)
        {
            try
            {
                var user = order.User;
                var address = order.Address;
                var orderDetails = order.OrderDetails;

                if (user == null || address == null)
                {
                    _logger.LogWarning($"Cannot send order status update email for OrderId={order.OrderId}. User or address not found.");
                    return;
                }

                var customerProfile = await _context.CustomerProfiles
                    .FirstOrDefaultAsync(c => c.CustomerId == order.UserId);
                var discountCode = customerProfile?.DiscountCode;
                var discountPercent = discountCode switch
                {
                    "GIAM5" => 5m,
                    "GIAM10" => 10m,
                    _ => 0m
                };
                var subTotal = orderDetails.Sum(od => od.UnitPrice * od.Quantity);
                var discountAmount = subTotal * discountPercent / 100;
                var discountLine = string.IsNullOrEmpty(discountCode) || discountAmount <= 0
                    ? ""
                    : $"<li><strong>Mã giảm giá ({discountCode}):</strong> -{discountAmount:N0}₫</li>";

                var productList = string.Join("", orderDetails.Select(od => $"<li>{od.Product.ProductName} (x{od.Quantity}) - {od.UnitPrice:N0}₫</li>"));
                var statusMessage = newStatus == "Đang giao hàng"
                    ? "Đơn hàng của bạn đang được giao đến địa chỉ của bạn. Vui lòng chuẩn bị để nhận hàng."
                    : "Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua sắm tại PouletLG!";

                var emailBody = $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
                  <tr>
                    <td align='center'>
                      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                        <tr>
                          <td style='padding-bottom: 10px;'>
                            <h2 style='color:#FECC29;'>Cập nhật trạng thái đơn hàng #{order.OrderId}</h2>
                          </td>
                        </tr>
                        <tr>
                          <td style='color:#333333; font-size:16px; line-height:1.6;'>
                            <p>Xin chào <strong>{user.FullName}</strong>,</p>
                            <p>Đơn hàng của bạn đã được cập nhật sang trạng thái: <strong>{newStatus}</strong>.</p>
                            <p>{statusMessage}</p>
                            <p><strong>Chi tiết đơn hàng:</strong></p>
                            <ul>
                                <li><strong>Mã đơn hàng:</strong> #{order.OrderId}</li>
                                <li><strong>Sản phẩm:</strong><ul>{productList}</ul></li>
                                {discountLine}
                                <li><strong>Phí vận chuyển:</strong> {order.ShippingFee:N0}₫</li>
                                <li><strong>Tổng thanh toán:</strong> {order.TotalAmount:N0}₫</li>
                                <li><strong>Địa chỉ giao hàng:</strong> {address.SpecificAddress}, {address.Ward}, {address.District}, {address.Province}</li>
                            </ul>
                            <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>";

                await _emailService.SendEmailAsync(user.Email, $"Cập nhật trạng thái đơn hàng #{order.OrderId} - PouletLG", emailBody);
                _logger.LogInformation($"Order status update email sent to {user.Email} for OrderId={order.OrderId}, NewStatus={newStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order status update email for OrderId={order.OrderId}, NewStatus={newStatus}: {ex.Message}");
            }
        }

        // 3. Lấy danh sách nhân viên giao hàng
        [HttpGet]
        public IActionResult GetDeliveryStaffs()
        {
            var staffs = _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Nhân viên giao hàng")
                .Select(u => new
                {
                    userId = u.UserId,
                    fullName = u.FullName
                }).ToList();

            return Json(staffs);
        }

        // 4. Gán nhân viên giao hàng cho đơn
        [HttpPost]
        public IActionResult AssignDeliveryStaff(int orderId, int shipperId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            var shipper = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == shipperId && u.Role.RoleName == "Nhân viên giao hàng");

            if (shipper == null)
                return BadRequest("Nhân viên giao hàng không tồn tại hoặc không hợp lệ.");

            order.ShipperId = shipperId;
            _context.SaveChanges();

            return Ok(new { success = true, message = "Gán nhân viên thành công!" });
        }

        // 5. Xem chi tiết đơn hàng
        public IActionResult Details(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.Shipper)
                .Where(o => o.OrderId == orderId)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    CustomerName = o.User.FullName,
                    CustomerEmail = o.User.Email,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    TotalAmount = o.TotalAmount,
                    ShippingFee = o.ShippingFee,
                    Discount = o.Discount,
                    FullAddress = $"{o.Address.SpecificAddress}, {o.Address.Ward}, {o.Address.District}, {o.Address.Province}",
                    ShipperName = o.Shipper != null ? o.Shipper.FullName : null,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product.ProductName,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        OriginalPrice = od.Product.Price,
                        IsDiscounted = od.UnitPrice < od.Product.Price
                    }).ToList()
                }).FirstOrDefault();

            if (order == null)
                return NotFound();

            return View(order);
        }

        // 6. Trang dành riêng cho nhân viên giao hàng: xem các đơn được giao cho họ đang ở trạng thái "Đang giao hàng"
        public IActionResult DeliveryStaff()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.ShipperId == userId && o.OrderStatus == "Đang giao hàng")
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User.FullName,
                    OrderDate = o.OrderDate,
                    OrderStatus = o.OrderStatus,
                    TotalAmount = o.TotalAmount,
                    ShippingFee = o.ShippingFee,
                    Discount = o.Discount,
                    FullAddress = $"{o.Address.SpecificAddress}, {o.Address.Ward}, {o.Address.District}, {o.Address.Province}",
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product.ProductName,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice,
                        OriginalPrice = od.Product.Price,
                        IsDiscounted = od.UnitPrice < od.Product.Price
                    }).ToList()
                }).ToList();

            return View(orders);
        }
    }
}
