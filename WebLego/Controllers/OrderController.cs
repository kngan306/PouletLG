using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;
using System;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using WebLego.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebLego.Services;
using System.Threading.Tasks;
using System.Linq;

namespace WebLego.Controllers
{
    public class OrderController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly IOptions<VNPayConfig> _vnpayConfig;
        private readonly ILogger _logger;
        private readonly EmailService _emailService;

        public OrderController(DbpouletLgv5Context context, IOptions<VNPayConfig> vnpayConfig, ILogger<OrderController> logger, EmailService emailService)
        {
            _context = context;
            _vnpayConfig = vnpayConfig;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Checkout(string selectedProductIds, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(selectedProductIds))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var selectedIds = selectedProductIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(idStr => int.TryParse(idStr, out int id) ? id : -1)
                .Where(id => id > 0)
                .ToList();

            var now = DateTime.Now;

            // 🧠 TH1: sản phẩm đã nằm trong Cart
            var cartEntities = _context.Carts
                .Where(c => c.UserId == userId && selectedIds.Contains(c.ProductId))
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToList();

            var cartItems = new List<CartItemViewModel>();

            if (cartEntities.Any())
            {
                cartItems = cartEntities.Select(c =>
                {
                    var originalPrice = c.Product.Price;
                    var discountPrice = (c.Product.DiscountPrice.HasValue)
                        ? c.Product.DiscountPrice.Value
                        : originalPrice;
                    var isDiscounted = discountPrice != originalPrice;

                    var imageUrl = c.Product.ProductImages
                        .FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? "";

                    return new CartItemViewModel
                    {
                        ProductId = c.ProductId,
                        ProductName = c.Product.ProductName,
                        ImageUrl = imageUrl,
                        OriginalPrice = originalPrice,
                        Price = discountPrice,
                        Quantity = c.Quantity,
                        Total = discountPrice * c.Quantity,
                        IsDiscounted = isDiscounted,
                        Savings = originalPrice - discountPrice
                    };
                }).ToList();
            }
            else
            {
                // 🧠 TH2: sản phẩm chưa nằm trong Cart — MUA NGAY
                var product = _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefault(p => p.ProductId == selectedIds.First());

                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                    return RedirectToAction("Index", "Cart");
                }

                var originalPrice = product.Price;
                var discountPrice = (product.DiscountPrice.HasValue)
                    ? product.DiscountPrice.Value
                    : originalPrice;

                var isDiscounted = discountPrice != originalPrice;

                var imageUrl = product.ProductImages
                    .FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? "";

                cartItems.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImageUrl = imageUrl,
                    OriginalPrice = originalPrice,
                    Price = discountPrice,
                    Quantity = quantity,
                    Total = discountPrice * quantity,
                    IsDiscounted = isDiscounted,
                    Savings = originalPrice - discountPrice
                });
            }

            // Lấy địa chỉ và thông tin giảm giá
            var addresses = _context.UserAddresses.Where(a => a.UserId == userId).ToList();
            var customerProfile = _context.CustomerProfiles.FirstOrDefault(c => c.CustomerId == userId);
            var discountCode = customerProfile?.DiscountCode;
            var discountPercent = discountCode switch
            {
                "GIAM5" => 5m,
                "GIAM10" => 10m,
                _ => 0m
            };

            var subTotal = cartItems.Sum(i => i.Total);
            var shippingFee = 30000m;
            var discountAmount = subTotal * discountPercent / 100;
            var total = subTotal + shippingFee - discountAmount;

            var checkoutModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                Addresses = addresses.Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    SpecificAddress = a.SpecificAddress,
                    Ward = a.Ward,
                    District = a.District,
                    Province = a.Province,
                    IsDefault = a.IsDefault == true
                }).ToList(),
                DiscountCode = discountCode,
                DiscountAmount = discountAmount,
                TotalAmount = total,
                ShippingFee = shippingFee
            };

            ViewBag.Addresses = checkoutModel.Addresses;
            ViewBag.DiscountCode = discountCode;
            ViewBag.CartItems = cartItems;
            ViewBag.SubTotal = subTotal;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Total = total;
            ViewBag.SelectedProductIds = string.Join(",", selectedIds);

            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault == true);
            if (defaultAddress != null)
            {
                ViewBag.DefaultAddressId = defaultAddress.AddressId;
            }

            return View(checkoutModel);
        }

        [HttpPost]
        public IActionResult Checkout([FromForm] int[] selectedProductIds)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            var selectedIds = selectedProductIds.ToList();

            var addresses = _context.UserAddresses
                .Where(a => a.UserId == userId)
                .ToList();

            var customerProfile = _context.CustomerProfiles
                .FirstOrDefault(c => c.CustomerId == userId);
            var discountCode = customerProfile?.DiscountCode;
            var discountPercent = customerProfile?.DiscountCode switch
            {
                "GIAM5" => 5m,
                "GIAM10" => 10m,
                _ => 0m
            };

            var now = DateTime.Now;

            var cartEntities = _context.Carts
                .Where(c => c.UserId == userId && selectedIds.Contains(c.ProductId))
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToList();

            if (!cartEntities.Any())
            {
                TempData["Error"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var cartItems = cartEntities.Select(c =>
            {
                var originalPrice = c.Product.Price;
                var discountPrice = (c.Product.DiscountPrice.HasValue)
                    ? c.Product.DiscountPrice.Value
                    : originalPrice;
                var isDiscounted = discountPrice != originalPrice;

                var imageUrl = c.Product.ProductImages
                    .FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? "";

                return new CartItemViewModel
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product.ProductName,
                    ImageUrl = imageUrl,
                    OriginalPrice = originalPrice,
                    Price = discountPrice,
                    Quantity = c.Quantity,
                    Total = discountPrice * c.Quantity,
                    IsDiscounted = isDiscounted,
                    Savings = originalPrice - discountPrice
                };
            }).ToList();

            var subTotal = cartItems.Sum(i => i.Price * i.Quantity);
            var shippingFee = 30000m;
            var discountAmount = subTotal * discountPercent / 100;
            var total = subTotal + shippingFee - discountAmount;

            var checkoutModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                Addresses = addresses.Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    SpecificAddress = a.SpecificAddress,
                    Ward = a.Ward,
                    District = a.District,
                    Province = a.Province,
                    IsDefault = a.IsDefault == true
                }).ToList(),
                DiscountCode = discountCode,
                DiscountAmount = discountAmount,
                TotalAmount = total,
                ShippingFee = shippingFee
            };

            ViewBag.Addresses = checkoutModel.Addresses;
            ViewBag.DiscountCode = discountCode;
            ViewBag.CartItems = cartItems;
            ViewBag.SubTotal = subTotal;
            ViewBag.ShippingFee = shippingFee;
            ViewBag.Total = total;
            ViewBag.SelectedProductIds = string.Join(",", selectedProductIds);

            var defaultAddress = addresses.FirstOrDefault(a => a.IsDefault == true);
            if (defaultAddress != null)
            {
                ViewBag.DefaultAddressId = defaultAddress.AddressId;
            }

            return View(checkoutModel);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(int? selectedAddressId, string paymentMethod, [FromForm] int[] selectedProductIds, decimal total, int? orderId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            if (orderId.HasValue)
            {
                var order = _context.Orders
                    .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId && o.OrderStatus == "Chờ thanh toán");
                if (order == null)
                    return RedirectToAction("Index");

                paymentMethod = order.PaymentMethod;
                selectedAddressId = order.AddressId;

                if (paymentMethod == "VNPAY")
                {
                    return RedirectToAction("CreatePayment", new { total = total, orderId = order.OrderId });
                }
                else if (paymentMethod == "COD")
                {
                    order.PaymentStatus = "Chưa thanh toán";
                    order.OrderStatus = "Đang xử lý";
                    await _context.SaveChangesAsync();

                    // Gửi email xác nhận đơn hàng
                    await SendOrderConfirmationEmail(order);
                    return View("OrderSuccess");
                }
            }
            else
            {
                if (selectedProductIds == null || selectedProductIds.Length == 0)
                    return RedirectToAction("Index", "Cart");

                var selectedIds = selectedProductIds.ToList();

                var cartItems = _context.Carts
                    .Where(c => c.UserId == userId && selectedIds.Contains(c.ProductId))
                    .Include(c => c.Product)
                    .ToList();

                if (!cartItems.Any())
                    return RedirectToAction("Index", "Cart");

                if (!selectedAddressId.HasValue)
                    return BadRequest("Địa chỉ không hợp lệ");

                var address = _context.UserAddresses
                    .FirstOrDefault(a => a.AddressId == selectedAddressId.Value && a.UserId == userId);
                if (address == null)
                    return BadRequest("Địa chỉ không hợp lệ");

                if (paymentMethod != "COD" && paymentMethod != "VNPAY")
                    return BadRequest("Phương thức thanh toán không hợp lệ");

                var customerProfile = _context.CustomerProfiles
                    .FirstOrDefault(c => c.CustomerId == userId);
                var discountPercent = customerProfile?.DiscountCode switch
                {
                    "GIAM5" => 5m,
                    "GIAM10" => 10m,
                    _ => 0m
                };

                var now = DateTime.Now;

                var cartItemsWithDetails = cartItems.Select(c =>
                {
                    var originalPrice = c.Product.Price;
                    var discountPrice = (c.Product.DiscountPrice.HasValue)
                        ? c.Product.DiscountPrice.Value
                        : originalPrice;
                    var isDiscounted = discountPrice != originalPrice;

                    return new
                    {
                        c.ProductId,
                        c.Quantity,
                        OriginalPrice = originalPrice,
                        IsDiscounted = isDiscounted,
                        Price = discountPrice,
                        Savings = originalPrice - discountPrice
                    };
                }).ToList();

                var subTotal = cartItemsWithDetails.Sum(i => i.Price * i.Quantity);
                var shippingFee = 30000m;
                var discountAmount = subTotal * discountPercent / 100;
                var calculatedTotal = subTotal + shippingFee - discountAmount;

                if (Math.Abs(total - calculatedTotal) > 0.01m)
                {
                    _logger.LogError("Tổng thanh toán không hợp lệ: total={Total}, calculatedTotal={CalculatedTotal}, subTotal={SubTotal}, discountAmount={DiscountAmount}, shippingFee={ShippingFee}", total, calculatedTotal, subTotal, discountAmount, shippingFee);
                    return BadRequest("Tổng thanh toán không hợp lệ");
                }

                var order = new Order
                {
                    UserId = userId.Value,
                    AddressId = selectedAddressId.Value,
                    OrderDate = DateTime.Now,
                    PaymentMethod = paymentMethod,
                    ShippingFee = shippingFee,
                    TotalAmount = total,
                    OrderStatus = paymentMethod == "VNPAY" ? "Chờ thanh toán" : "Đang xử lý",
                    PaymentStatus = paymentMethod == "VNPAY" ? "Chưa thanh toán" : "Chưa thanh toán"
                };

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Lưu Order trước

                    foreach (var item in cartItemsWithDetails)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.OrderId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.Price
                        };
                        _context.OrderDetails.Add(orderDetail);
                    }
                    await _context.SaveChangesAsync(); // Lưu OrderDetails

                    // Giảm StockQuantity sau khi lưu OrderDetails
                    foreach (var item in cartItemsWithDetails)
                    {
                        var product = _context.Products.Find(item.ProductId);
                        if (product != null && (order.OrderStatus == "Chờ thanh toán" || order.OrderStatus == "Đang xử lý"))
                        {
                            product.StockQuantity -= item.Quantity;
                        }
                    }
                    await _context.SaveChangesAsync(); // Cập nhật StockQuantity

                    _context.Carts.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Gửi email xác nhận đơn hàng cho COD
                    if (paymentMethod == "COD")
                    {
                        await SendOrderConfirmationEmail(order);
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi tạo đơn hàng: {Message}", ex.Message);
                    return Json(new { success = false, message = $"Lỗi khi tạo đơn hàng: {ex.Message}" });
                }

                HttpContext.Session.SetInt32("UserId", userId.Value);

                if (paymentMethod == "VNPAY")
                {
                    return RedirectToAction("CreatePayment", new { total = total, orderId = order.OrderId });
                }
            }

            return View("OrderSuccess");
        }

        public IActionResult CreatePayment(decimal total, int orderId)
        {
            var vnpay = _vnpayConfig.Value;
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("UserId is null in session for CreatePayment, orderId: {OrderId}", orderId);
                return RedirectToAction("Login", "Auth");
            }

            var amount = total;
            if (amount <= 0)
            {
                _logger.LogWarning("Invalid amount {Amount} for orderId: {OrderId}", amount, orderId);
                return RedirectToAction("Checkout");
            }

            var orderInfo = $"Thanh toan don hang WebLego cho UserId: {userId}";
            var returnUrl = vnpay.ReturnUrl;
            var tmnCode = vnpay.TmnCode;
            var hashSecret = vnpay.HashSecret;
            var vnpUrl = vnpay.Url;
            var createDate = DateTime.Now;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var txnRef = $"{orderId}_{DateTime.Now:yyyyMMddHHmmssfff}";

            var data = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((int)(amount * 100)).ToString() },
                { "vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", Uri.EscapeDataString(orderInfo) },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", txnRef }
            };

            var queryString = string.Join("&", data.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var secureHash = CreateSecureHash(queryString, hashSecret);

            var paymentUrl = $"{vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";
            _logger.LogInformation("Payment URL for orderId {OrderId}: {PaymentUrl}", orderId, paymentUrl);
            _logger.LogInformation("QueryString for orderId {OrderId}: {QueryString}", orderId, queryString);
            _logger.LogInformation("SecureHash for orderId {OrderId}: {SecureHash}", orderId, secureHash);

            Response.Headers.Add("ngrok-skip-browser-warning", "any-value");
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> VnpayReturn()
        {
            var vnpay = _vnpayConfig.Value;
            _logger.LogInformation("VnpayReturn called at: {Time}", DateTime.Now);
            foreach (var key in Request.Query.Keys)
            {
                _logger.LogInformation("VnpayReturn {Key}: {Value}", key, Request.Query[key]);
            }

            var txnRef = Request.Query["vnp_TxnRef"].ToString();
            if (string.IsNullOrEmpty(txnRef))
            {
                _logger.LogWarning("vnp_TxnRef is null or empty");
                return View("PaymentFailed");
            }

            var orderIdPart = txnRef.Split('_')[0];
            if (!int.TryParse(orderIdPart, out int orderId))
            {
                _logger.LogWarning("Invalid orderId format in txnRef: {TxnRef}", txnRef);
                return View("PaymentFailed");
            }

            var amount = decimal.Parse(Request.Query["vnp_Amount"].ToString()) / 100;
            var userId = HttpContext.Session.GetInt32("UserId");
            _logger.LogInformation("Current UserId from Session: {UserId}", userId);

            var order = _context.Orders
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for orderId: {OrderId}", orderId);
                return View("PaymentFailed");
            }

            if (Request.Query["vnp_ResponseCode"] == "00")
            {
                _logger.LogInformation("Payment successful for orderId: {OrderId}, UserId: {UserId}", orderId, userId);
                if (userId == null)
                {
                    userId = order.UserId;
                    if (userId.HasValue)
                    {
                        HttpContext.Session.SetInt32("UserId", userId.Value);
                        _logger.LogInformation("Restored UserId: {UserId} for orderId: {OrderId}", userId.Value, orderId);
                    }
                    else
                    {
                        _logger.LogWarning("UserId is null in order for orderId: {OrderId}", orderId);
                        return View("PaymentFailed");
                    }
                }
                order.OrderStatus = "Đang xử lý";
                order.PaymentStatus = "Đã thanh toán";
                order.OrderDate = DateTime.Now;

                order.VnpTransactionNo = Request.Query["vnp_TransactionNo"];
                var vnpTransactionDate = Request.Query["vnp_TransactionDate"];
                if (string.IsNullOrEmpty(vnpTransactionDate))
                {
                    _logger.LogWarning("vnp_TransactionDate is null or empty for orderId: {OrderId}. Using fallback date.", orderId);
                    order.VnpTransactionDate = DateTime.Now;
                }
                else
                {
                    try
                    {
                        order.VnpTransactionDate = DateTime.ParseExact(vnpTransactionDate, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning(ex, "Invalid vnp_TransactionDate format: {TransactionDate} for orderId: {OrderId}. Using fallback date.", vnpTransactionDate, orderId);
                        order.VnpTransactionDate = DateTime.Now;
                    }
                }
                await _context.SaveChangesAsync();

                // Gửi email xác nhận đơn hàng
                await SendOrderConfirmationEmail(order);

                var vnpTransactionNo = Request.Query["vnp_TransactionNo"];
                if (!string.IsNullOrEmpty(vnpTransactionNo))
                {
                    HttpContext.Session.SetString("VnpTransactionNo_" + orderId, vnpTransactionNo);
                    _logger.LogInformation("Set VnpTransactionNo: {TransactionNo} for orderId: {OrderId}", vnpTransactionNo, orderId);
                }
                if (order.VnpTransactionDate.HasValue)
                {
                    HttpContext.Session.SetString("VnpTransactionDate_" + orderId, order.VnpTransactionDate.Value.ToString("yyyyMMddHHmmss"));
                    _logger.LogInformation("Set VnpTransactionDate: {TransactionDate} for orderId: {OrderId}", order.VnpTransactionDate.Value, orderId);
                }
                else
                {
                    _logger.LogWarning("Failed to set VnpTransactionDate for orderId: {OrderId}", orderId);
                }
                return View("PaymentSuccess");
            }
            else
            {
                _logger.LogInformation("Payment failed for orderId: {OrderId}, ResponseCode: {ResponseCode}", orderId, Request.Query["vnp_ResponseCode"]);
                if (userId == null)
                {
                    userId = order.UserId;
                    if (userId.HasValue)
                    {
                        HttpContext.Session.SetInt32("UserId", userId.Value);
                        _logger.LogInformation("Restored UserId: {UserId} for orderId: {OrderId} on failure", userId.Value, orderId);
                    }
                    else
                    {
                        _logger.LogWarning("UserId is null in order for orderId: {OrderId} on failure", orderId);
                        return View("PaymentFailed");
                    }
                }

                TempData["OrderId"] = orderId;
                TempData["ExpirationDate"] = order.OrderDate?.AddHours(24);
                TempData.Keep("OrderId");
                TempData.Keep("ExpirationDate");
                return View("PaymentFailed");
            }
        }

        private async Task SendOrderConfirmationEmail(Order order)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == order.UserId);
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressId == order.AddressId);
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.OrderId == order.OrderId)
                    .ToListAsync();

                if (user == null || address == null)
                {
                    _logger.LogWarning($"Cannot send order confirmation email for OrderId={order.OrderId}. User or address not found.");
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
                var emailBody = $@"
                <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
                  <tr>
                    <td align='center'>
                      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                        <tr>
                          <td style='padding-bottom: 10px;'>
                            <h2 style='color:#FECC29;'>Đơn hàng của bạn đã được đặt thành công!</h2>
                          </td>
                        </tr>
                        <tr>
                          <td style='color:#333333; font-size:16px; line-height:1.6;'>
                            <p>Xin chào <strong>{user.FullName}</strong>,</p>
                            <p>Cảm ơn bạn đã đặt hàng tại PouletLG! Đơn hàng của bạn đã được ghi nhận.</p>
                            <p><strong>Chi tiết đơn hàng:</strong></p>
                            <ul>
                                <li><strong>Mã đơn hàng:</strong> #{order.OrderId}</li>
                                <li><strong>Sản phẩm:</strong><ul>{productList}</ul></li>
                                {discountLine}
                                <li><strong>Phí vận chuyển:</strong> {order.ShippingFee:N0}₫</li>
                                <li><strong>Tổng thanh toán:</strong> {order.TotalAmount:N0}₫</li>
                                <li><strong>Địa chỉ giao hàng:</strong> {address.SpecificAddress}, {address.Ward}, {address.District}, {address.Province}</li>
                            </ul>
                            <p>Chúng tôi sẽ sớm xử lý và giao hàng đến bạn. Vui lòng chuẩn bị thanh toán nếu chọn thanh toán khi nhận hàng.</p>            
                            <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>";

                await _emailService.SendEmailAsync(user.Email, $"Đơn hàng #{order.OrderId} - PouletLG", emailBody);
                _logger.LogInformation($"Order confirmation email sent to {user.Email} for OrderId={order.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order confirmation email for OrderId={order.OrderId}: {ex.Message}");
            }
        }

        public IActionResult RetryPayment(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null || order.OrderStatus != "Chờ thanh toán" || !order.OrderDate.HasValue)
            {
                TempData["Error"] = "Đơn hàng không hợp lệ hoặc đã hết thời gian thanh toán.";
                return RedirectToAction("Index");
            }

            var expirationDate = order.OrderDate.Value.AddHours(24);
            var expirationSeconds = expirationDate > DateTime.Now ? (expirationDate - DateTime.Now).TotalSeconds : 0;

            if (expirationSeconds <= 0)
            {
                order.OrderStatus = "Đã hủy";
                _context.SaveChanges();
                TempData["Error"] = "Đơn hàng đã hết thời gian thanh toán.";
                return RedirectToAction("Index");
            }

            var orderDate = order.OrderDate ?? DateTime.Now;
            var subTotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            var shippingFee = order.ShippingFee ?? 30000m;
            var totalAmount = order.TotalAmount ?? 0m; // Xử lý null cho TotalAmount
            var expectedTotal = subTotal + shippingFee;
            var discountAmount = expectedTotal > totalAmount ? expectedTotal - totalAmount : 0m;
            var discountPercent = discountAmount > 0 ? (discountAmount / subTotal * 100) : 0m;
            var discountCode = discountPercent switch
            {
                5m => "GIAM5",
                10m => "GIAM10",
                _ => ""
            };

            var cartItems = order.OrderDetails.Select(od =>
            {
                var originalPrice = od.Product.Price;
                var discountPrice = (od.Product.DiscountPrice.HasValue /*&& od.Product.PromotionStartDate <= orderDate && od.Product.PromotionEndDate >= orderDate*/)
                    ? od.Product.DiscountPrice.Value
                    : originalPrice;
                var isDiscounted = discountPrice != originalPrice;

                return new CartItemViewModel
                {
                    ProductId = od.ProductId,
                    ProductName = od.Product.ProductName,
                    ImageUrl = od.Product.ProductImages.FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl ?? "",
                    OriginalPrice = originalPrice,
                    Price = discountPrice,
                    Quantity = od.Quantity,
                    Total = discountPrice * od.Quantity,
                    IsDiscounted = isDiscounted,
                    Savings = originalPrice - discountPrice
                };
            }).ToList();

            var checkoutModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                TotalAmount = totalAmount,
                Addresses = _context.UserAddresses.Where(a => a.UserId == userId).ToList().Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    SpecificAddress = a.SpecificAddress,
                    Ward = a.Ward,
                    District = a.District,
                    Province = a.Province,
                    IsDefault = a.IsDefault == true
                }).ToList(),
                DiscountCode = discountCode,
                DiscountAmount = discountAmount,
                ShippingFee = shippingFee
            };

            ViewBag.ExpirationTime = expirationSeconds;
            ViewBag.OriginalPaymentMethod = order.PaymentMethod;
            ViewBag.OriginalAddressId = order.AddressId;
            TempData["OrderId"] = orderId;

            return View("Checkout", checkoutModel);
        }

        private string CreateSecureHash(string data, string key)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public IActionResult Index(string statusFilter = "all")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var pendingOrders = _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Chờ thanh toán" && o.OrderDate.HasValue && o.OrderDate.Value.AddHours(24) < DateTime.Now)
                .ToList();
            foreach (var order in pendingOrders)
            {
                order.OrderStatus = "Đã hủy";
                HttpContext.Session.Remove("VnpTransactionNo_" + order.OrderId);
                HttpContext.Session.Remove("VnpTransactionDate_" + order.OrderId);
                _context.SaveChanges();
            }

            var query = _context.Orders
                .Where(o => o.UserId == userId);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
            {
                query = query.Where(o => o.OrderStatus.ToLower() == statusFilter.ToLower());
            }

            var orders = query
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var orderViewModels = orders.Select(o =>
            {
                var orderDate = o.OrderDate ?? DateTime.Now;
                var subTotal = o.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
                var shippingFee = o.ShippingFee ?? 30000m;
                var totalAmount = o.TotalAmount ?? 0m; // Xử lý null cho TotalAmount
                var expectedTotal = subTotal + shippingFee;
                var discountAmount = expectedTotal > totalAmount ? expectedTotal - totalAmount : 0m;
                var discountPercent = discountAmount > 0 ? (discountAmount / subTotal * 100) : 0m;
                var discountCode = discountPercent switch
                {
                    5m => "GIAM5",
                    10m => "GIAM10",
                    _ => ""
                };

                return new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderDate = o.OrderDate ?? DateTime.MinValue,
                    OrderStatus = o.OrderStatus,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    TotalAmount = totalAmount,
                    ShippingFee = shippingFee,
                    ExpirationDate = o.OrderStatus == "Chờ thanh toán" && o.OrderDate.HasValue ? o.OrderDate.Value.AddHours(24) : null,
                    VnpTransactionNo = HttpContext.Session.GetString("VnpTransactionNo_" + o.OrderId),
                    VnpTransactionDate = o.VnpTransactionDate,
                    DiscountCode = discountCode,
                    DiscountAmount = discountAmount,
                    Items = o.OrderDetails.Select(od =>
                    {
                        var originalPrice = od.Product.Price;
                        var discountPrice = (od.Product.DiscountPrice.HasValue /*&& od.Product.PromotionStartDate <= orderDate && od.Product.PromotionEndDate >= orderDate*/)
                            ? od.Product.DiscountPrice.Value
                            : originalPrice;
                        var isDiscounted = discountPrice != originalPrice;
                        var mainImage = od.Product.ProductImages
                            .FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl ?? "";
                        return new OrderItemViewModel
                        {
                            ProductId = od.ProductId,
                            ProductName = od.Product.ProductName,
                            Quantity = od.Quantity,
                            OriginalPrice = originalPrice,
                            DiscountPrice = isDiscounted ? discountPrice : null,
                            IsDiscounted = isDiscounted,
                            Price = od.UnitPrice,
                            ImageUrl = mainImage,
                            Savings = originalPrice - discountPrice
                        };
                    }).ToList()
                };
            }).ToList();

            ViewBag.StatusFilter = statusFilter;
            return View(orderViewModels);
        }

        public IActionResult Detail(int orderId)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            // Lấy thông tin đơn hàng
            var order = _context.Orders
                .Where(o => o.OrderId == orderId && o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(o => o.CommunityPosts)
                    .ThenInclude(p => p.Product)
                .Include(o => o.CommunityPosts)
                    .ThenInclude(p => p.User)
                .Include(o => o.CommunityPosts)
                    .ThenInclude(p => p.Comments)
                        .ThenInclude(c => c.User)
                .Include(o => o.CommunityPosts)
                    .ThenInclude(p => p.ContestVotes)
                .Include(o => o.CommunityPosts)
                    .ThenInclude(p => p.Contest)
                .FirstOrDefault();

            if (order == null) return NotFound();

            var orderDate = order.OrderDate ?? DateTime.Now;
            var subTotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
            var shippingFee = order.ShippingFee ?? 30000m;
            var totalAmount = order.TotalAmount ?? 0m;
            var expectedTotal = subTotal + shippingFee;
            var discountAmount = expectedTotal > totalAmount ? expectedTotal - totalAmount : 0m;
            var discountPercent = discountAmount > 0 ? (discountAmount / subTotal * 100) : 0m;
            var discountCode = discountPercent switch
            {
                5m => "GIAM5",
                10m => "GIAM10",
                _ => ""
            };

            // Kiểm tra cuộc thi đang hoạt động
            var activeContest = _context.Contests
                .FirstOrDefault(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now);

            // Kiểm tra điều kiện hiển thị nút Chia sẻ
            bool canShare = order.OrderStatus == "Hoàn thành" &&
                           order.OrderDate.HasValue &&
                           order.OrderDate.Value.AddDays(30) >= DateTime.Now &&
                           activeContest != null;

            // Lấy danh sách đánh giá
            var reviews = _context.ProductReviews
                .Include(r => r.Product)
                .Where(r => r.OrderId == orderId && r.UserId == userId.Value)
                .Select(r => new OrderReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductId,
                    ProductName = r.Product.ProductName,
                    Rating = r.Rating ?? 0,
                    Comment = r.Comment,
                    ImageUrl = r.ImageUrl,
                    CreatedAt = r.CreatedAt,
                    AdminReply = r.AdminReply,
                    AdminReplyAt = r.AdminReplyAt,
                    IsFlagged = (bool)r.IsFlagged,
                    IsUpdated = r.IsUpdated,
                    UpdatedAt = r.UpdatedAt
                }).ToList();

            // Tạo view model cho đơn hàng
            var orderViewModel = new OrderViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate ?? DateTime.MinValue,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                TotalAmount = totalAmount,
                ShippingFee = shippingFee,
                ExpirationDate = order.OrderStatus == "Chờ thanh toán" && order.OrderDate.HasValue ? order.OrderDate.Value.AddHours(24) : null,
                VnpTransactionNo = HttpContext.Session.GetString("VnpTransactionNo_" + orderId),
                VnpTransactionDate = order.VnpTransactionDate,
                DiscountCode = discountCode,
                DiscountAmount = discountAmount,
                Reviews = reviews,
                Items = order.OrderDetails.Select(od =>
                {
                    var originalPrice = od.Product.Price;
                    var discountPrice = (od.Product.DiscountPrice.HasValue)
                        ? od.Product.DiscountPrice.Value
                        : originalPrice;
                    var isDiscounted = discountPrice != originalPrice;

                    var mainImage = od.Product.ProductImages
                        .FirstOrDefault(pi => pi.IsMain == true)?.ImageUrl ?? "~/images/placeholder.png";

                    var hasReviewed = _context.ProductReviews
                        .Any(r => r.UserId == userId.Value && r.ProductId == od.ProductId && r.OrderId == order.OrderId);

                    return new OrderItemViewModel
                    {
                        ProductId = od.ProductId,
                        ProductName = od.Product.ProductName,
                        Quantity = od.Quantity,
                        OriginalPrice = originalPrice,
                        DiscountPrice = isDiscounted ? discountPrice : null,
                        IsDiscounted = isDiscounted,
                        Price = od.UnitPrice,
                        ImageUrl = mainImage,
                        Savings = originalPrice - discountPrice,
                        HasReviewed = hasReviewed
                    };
                }).ToList(),
                Address = _context.UserAddresses
                    .Where(a => a.AddressId == order.AddressId && a.UserId == userId)
                    .Select(a => new AddressViewModel
                    {
                        AddressId = a.AddressId,
                        FullName = a.FullName,
                        Phone = a.Phone,
                        SpecificAddress = a.SpecificAddress,
                        Ward = a.Ward,
                        District = a.District,
                        Province = a.Province,
                        IsDefault = a.IsDefault == true
                    }).FirstOrDefault(),
                CommunityPosts = order.CommunityPosts.Select(p => new CommunityPostViewModel
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    UserName = p.User?.FullName ?? "Không xác định",
                    OrderId = p.OrderId,
                    ProductId = p.ProductId,
                    ProductName = p.Product.ProductName,
                    ContestId = p.ContestId,
                    ContestTitle = p.Contest != null ? p.Contest.Title : null,
                    ImageUrl = p.ImageUrl,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    CommentCount = p.Comments.Count,
                    VoteCount = p.ContestVotes.Count,
                    IsVoted = userId.HasValue && p.ContestVotes.Any(v => v.UserId == userId.Value),
                    IsOwner = userId.HasValue && p.UserId == userId.Value,
                    IsFlagged = p.IsFlagged,
                    Comments = p.Comments.Select(c => new CommunityCommentViewModel
                    {
                        CommentId = c.CommentId,
                        UserId = c.UserId,
                        UserName = c.User?.FullName ?? "Không xác định",
                        CommentText = c.CommentText,
                        CreatedAt = c.CreatedAt,
                        IsFlagged = c.IsFlagged
                    }).ToList()
                }).ToList()
            };

            // Truyền trạng thái cuộc thi vào ViewBag
            ViewBag.CanShare = canShare;
            ViewBag.ActiveContest = activeContest != null ? new { activeContest.ContestId, activeContest.Title } : null;

            return View(orderViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("UserId is null in session for CancelOrder, orderId: {OrderId}", orderId);
                return RedirectToAction("Login", "Auth");
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId);
            if (order == null)
            {
                _logger.LogWarning("Order not found for orderId: {OrderId}, userId: {UserId}", orderId, userId);
                return NotFound();
            }

            if (order.OrderStatus == "Chờ thanh toán" || order.OrderStatus == "Đang xử lý")
            {
                _logger.LogInformation("Attempting to cancel orderId: {OrderId}, current PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                if (order.PaymentMethod == "VNPAY" && order.PaymentStatus == "Đã thanh toán")
                {
                    var vnpTransactionNo = HttpContext.Session.GetString("VnpTransactionNo_" + orderId) ?? order.VnpTransactionNo;
                    var vnpTransactionDate = order.VnpTransactionDate;

                    if (string.IsNullOrEmpty(vnpTransactionNo) || !vnpTransactionDate.HasValue)
                    {
                        _logger.LogWarning("Missing transaction data - vnpTransactionNo: {TransactionNo}, vnpTransactionDate: {TransactionDate} for orderId: {OrderId}", vnpTransactionNo, vnpTransactionDate, orderId);
                        order.OrderStatus = "Đã hủy";
                        _context.SaveChanges();
                        _logger.LogInformation("Order canceled for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                        TempData["Message"] = "Đơn hàng đã được hủy. Hoàn tiền không thể thực hiện do thiếu thông tin giao dịch.";
                        return RedirectToAction("Index");
                    }

                    var vnpay = _vnpayConfig.Value;
                    if (vnpay.SimulateRefund)
                    {
                        _logger.LogInformation("Refund simulation enabled for orderId: {OrderId}. Cannot cancel order due to refund unavailability.", orderId);
                        TempData["Error"] = "Đơn hàng không hủy được do tính năng hoàn tiền không khả dụng trong môi trường thử nghiệm.";
                        return RedirectToAction("Index");
                    }

                    if (string.IsNullOrEmpty(vnpay.RefundUrl))
                    {
                        _logger.LogError("RefundUrl is not configured in appsettings.json for orderId: {OrderId}", orderId);
                        TempData["Error"] = "RefundUrl không được cấu hình trong appsettings.json.";
                        return RedirectToAction("Index");
                    }

                    if (order.OrderDate.HasValue && (DateTime.Now - order.OrderDate.Value).TotalDays > 15)
                    {
                        _logger.LogWarning("Refund not allowed as transaction is older than 15 days for orderId: {OrderId}", orderId);
                        TempData["Error"] = "Không thể hoàn tiền vì giao dịch đã quá 15 ngày.";
                        return RedirectToAction("Index");
                    }

                    var amount = order.TotalAmount * 100;
                    var refundData = new SortedDictionary<string, string>
                    {
                        { "vnp_Version", "2.1.0" },
                        { "vnp_Command", "refund" },
                        { "vnp_TmnCode", vnpay.TmnCode },
                        { "vnp_Amount", ((int)amount).ToString() },
                        { "vnp_TransactionNo", vnpTransactionNo },
                        { "vnp_TransactionDate", vnpTransactionDate.Value.ToString("yyyyMMddHHmmss") },
                        { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                        { "vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" },
                        { "vnp_OrderInfo", Uri.EscapeDataString($"Hoan tien don hang {orderId} cho UserId {userId}") }
                    };

                    var queryString = string.Join("&", refundData.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    var secureHash = CreateSecureHash(queryString, vnpay.HashSecret);
                    refundData.Add("vnp_SecureHash", secureHash);

                    _logger.LogInformation("Refund QueryString for orderId {OrderId}: {QueryString}", orderId, queryString);
                    _logger.LogInformation("Refund SecureHash for orderId {OrderId}: {SecureHash}", orderId, secureHash);
                    _logger.LogInformation("Refund Data for orderId {OrderId}: {RefundData}", orderId, JsonSerializer.Serialize(refundData));

                    using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
                    {
                        var content = new FormUrlEncodedContent(refundData);
                        var response = await client.PostAsync(vnpay.RefundUrl, content);
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Refund Response Status for orderId {OrderId}: {StatusCode}, Content: {ResponseContent}", orderId, response.StatusCode, responseContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Refund failed with status: {StatusCode}, content: {ResponseContent} for orderId: {OrderId}", response.StatusCode, responseContent, orderId);
                            order.OrderStatus = "Đã hủy";
                            _context.SaveChanges();
                            _logger.LogInformation("Order canceled for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                            TempData["Message"] = $"Đơn hàng đã được hủy. Hoàn tiền không thành công do lỗi: {response.StatusCode} - {responseContent}";
                            return RedirectToAction("Index");
                        }

                        try
                        {
                            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent);
                            if (result == null || !result.ContainsKey("vnp_ResponseCode"))
                            {
                                _logger.LogWarning("VNPAY response does not contain vnp_ResponseCode for orderId: {OrderId}, content: {ResponseContent}", orderId, responseContent);
                                order.OrderStatus = "Đã hủy";
                                _context.SaveChanges();
                                _logger.LogInformation("Order canceled for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                                TempData["Message"] = $"Đơn hàng đã được hủy. Phản hồi từ VNPAY không hợp lệ: {responseContent}";
                                return RedirectToAction("Index");
                            }

                            switch (result["vnp_ResponseCode"])
                            {
                                case "00":
                                    order.OrderStatus = "Đã hủy";
                                    order.PaymentStatus = "Đã hoàn tiền";
                                    HttpContext.Session.Remove("VnpTransactionNo_" + orderId);
                                    HttpContext.Session.Remove("VnpTransactionDate_" + orderId);
                                    _context.SaveChanges();
                                    _logger.LogInformation("Refund successful for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                                    TempData["Message"] = "Đơn hàng đã được hủy và hoàn tiền thành công.";
                                    break;
                                case "01":
                                    _logger.LogWarning("Transaction does not exist for orderId: {OrderId}", orderId);
                                    TempData["Error"] = "Giao dịch không tồn tại.";
                                    break;
                                case "02":
                                    _logger.LogWarning("Transaction already refunded for orderId: {OrderId}", orderId);
                                    TempData["Error"] = "Giao dịch đã được hoàn tiền trước đó.";
                                    break;
                                case "04":
                                    _logger.LogWarning("Transaction not eligible for refund for orderId: {OrderId}", orderId);
                                    TempData["Error"] = "Giao dịch không đủ điều kiện để hoàn tiền.";
                                    break;
                                case "91":
                                    _logger.LogWarning("Transaction not found or pending for orderId: {OrderId}", orderId);
                                    TempData["Error"] = "Không tìm thấy giao dịch hoặc giao dịch đang chờ xử lý.";
                                    break;
                                case "94":
                                    _logger.LogWarning("Refund request sent but pending for orderId: {OrderId}", orderId);
                                    TempData["Error"] = "Yêu cầu hoàn tiền đã được gửi nhưng đang chờ xử lý.";
                                    break;
                                default:
                                    _logger.LogWarning("Refund failed with response code: {ResponseCode}, message: {Message} for orderId: {OrderId}", result["vnp_ResponseCode"], result["vnp_Message"] ?? "Unknown error", orderId);
                                    TempData["Error"] = $"Hoàn tiền thất bại. Mã lỗi: {result["vnp_ResponseCode"]}, Thông điệp: {result["vnp_Message"] ?? "Lỗi không xác định"}";
                                    break;
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "JSON Deserialize Error for orderId {OrderId}, Content: {ResponseContent}", orderId, responseContent);
                            order.OrderStatus = "Đã hủy";
                            _context.SaveChanges();
                            _logger.LogInformation("Order canceled for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                            TempData["Message"] = $"Đơn hàng đã được hủy. Lỗi phân tích phản hồi từ VNPAY: {responseContent}";
                        }
                    }
                }
                else
                {
                    order.OrderStatus = "Đã hủy";
                    if (order.PaymentMethod == "VNPAY" && !string.IsNullOrEmpty(HttpContext.Session.GetString("VnpTransactionNo_" + orderId)))
                    {
                        HttpContext.Session.Remove("VnpTransactionNo_" + orderId);
                        HttpContext.Session.Remove("VnpTransactionDate_" + orderId);
                    }
                    _context.SaveChanges();
                    _logger.LogInformation("Order canceled successfully for orderId: {OrderId}, PaymentStatus: {PaymentStatus}", orderId, order.PaymentStatus);
                    TempData["Message"] = "Đơn hàng đã được hủy thành công.";
                }

                return RedirectToAction("Index");
            }

            _logger.LogWarning("Cannot cancel order in current status: {OrderStatus} for orderId: {OrderId}", order.OrderStatus, orderId);
            TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult IPN()
        {
            var vnpay = _vnpayConfig.Value;
            var response = new Dictionary<string, string>();
            foreach (var key in Request.Form.Keys)
            {
                response[key] = Request.Form[key];
                _logger.LogInformation("IPN {Key}: {Value}", key, Request.Form[key]);
            }

            var secureHash = Request.Form["vnp_SecureHash"];
            var dataToVerify = Request.Form
                .Where(k => k.Key != "vnp_SecureHash")
                .OrderBy(k => k.Key)
                .Select(k => $"{k.Key}={Uri.EscapeDataString(k.Value)}")
                .Aggregate((a, b) => $"{a}&{b}");
            var computedHash = CreateSecureHash(dataToVerify, vnpay.HashSecret);
            if (secureHash != computedHash)
            {
                _logger.LogWarning("Invalid vnp_SecureHash in IPN");
                return BadRequest("Invalid secure hash");
            }

            return Ok("OK");
        }
    }
}