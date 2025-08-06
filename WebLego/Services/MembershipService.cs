using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebLego.DataSet.GdrService;
using WebLego.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace WebLego.Services
{
    public class MembershipService
    {
        private readonly DbpouletLgv5Context _context;
        private readonly EmailService _emailService;
        private readonly ILogger<MembershipService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(7); // Lưu cache 7 ngày

        public MembershipService(DbpouletLgv5Context context, EmailService emailService, ILogger<MembershipService> logger, IMemoryCache cache)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _cache = cache;
        }

        // Kiểm tra và cập nhật hạng thành viên, gửi email nếu thăng hạng
        public async Task CheckAndUpdateMembershipAsync(int userId)
        {
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
            if (customer == null)
            {
                _logger.LogWarning($"Customer profile not found for UserId={userId}");
                return;
            }

            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                .SumAsync(o => o.TotalAmount) ?? 0;

            var currentRank = customer.CustomerRank;
            var newRank = DetermineRank(totalSpent);
            var discountCode = newRank switch
            {
                "Bạc" => "GIAM5",
                "Vàng" => "GIAM10",
                _ => null
            };

            _logger.LogInformation($"Checking membership for UserId={userId}: TotalSpent={totalSpent:N0}, CurrentRank={currentRank}, NewRank={newRank}");

            if (currentRank != newRank)
            {
                var cacheKey = $"PromotionEmail_{userId}_{newRank}";
                if (!_cache.TryGetValue(cacheKey, out DateTime _))
                {
                    customer.CustomerRank = newRank;
                    customer.DiscountCode = discountCode;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"UserId={userId} upgraded from {currentRank} to {newRank}");

                    await SendPromotionEmailAsync(userId, newRank);
                    _cache.Set(cacheKey, DateTime.Now, _cacheDuration);
                }
                else
                {
                    _logger.LogInformation($"Promotion email for UserId={userId}, NewRank={newRank} already sent, skipping.");
                }
            }
            else
            {
                _logger.LogInformation($"No rank change for UserId={userId}, CurrentRank={currentRank}");
            }

            // Kiểm tra ngưỡng 30% sau khi cập nhật hạng
            await CheckMilestoneAndSendEmailAsync(userId, totalSpent);
        }

        // Kiểm tra và gửi email khi người dùng đạt 70%-100% ngưỡng hạng tiếp theo
        public async Task CheckMilestoneAndSendEmailAsync(int userId, decimal totalSpent)
        {
            var customer = await _context.CustomerProfiles
                .FirstOrDefaultAsync(c => c.CustomerId == userId);
            if (customer == null)
            {
                _logger.LogWarning($"Customer profile not found for UserId={userId}");
                return;
            }

            var currentRank = customer.CustomerRank;
            var (nextRank, requiredAmount) = GetNextRankInfo(currentRank);

            if (nextRank == null) // Đã ở hạng Vàng, không có hạng tiếp theo
            {
                _logger.LogInformation($"UserId={userId} is already at highest rank (Vàng), no milestone check needed.");
                return;
            }

            var percentageToNextRank = totalSpent / requiredAmount * 100;
            var cacheKey = $"MilestoneEmail_{userId}_{nextRank}";

            _logger.LogInformation($"Checking milestone for UserId={userId}: TotalSpent={totalSpent:N0}, NextRank={nextRank}, Percentage={percentageToNextRank:F2}%");

            // Kiểm tra cache để xem đã gửi email cho ngưỡng này chưa
            if (percentageToNextRank >= 70 && percentageToNextRank < 100 &&
                !_cache.TryGetValue(cacheKey, out DateTime _))
            {
                await SendMilestoneEmailAsync(userId, nextRank, requiredAmount, totalSpent);
                // Lưu vào cache với thời hạn 7 ngày
                _cache.Set(cacheKey, DateTime.Now, _cacheDuration);
                _logger.LogInformation($"Milestone email sent to UserId={userId} for next rank {nextRank}");
            }
        }

        // Xác định hạng thành viên dựa trên tổng chi tiêu
        private string DetermineRank(decimal totalSpent)
        {
            if (totalSpent >= 10000000) return "Vàng";
            if (totalSpent >= 5000000) return "Bạc";
            return "Đồng";
        }

        // Lấy thông tin hạng tiếp theo
        private (string? NextRank, decimal RequiredAmount) GetNextRankInfo(string currentRank)
        {
            return currentRank switch
            {
                "Đồng" => ("Bạc", 5000000m),
                "Bạc" => ("Vàng", 10000000m),
                "Vàng" => (null, 0m),
                _ => (null, 0m)
            };
        }

        // Gửi email chúc mừng thăng hạng
        private async Task SendPromotionEmailAsync(int userId, string newRank)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for UserId={userId}");
                    return;
                }

                var totalSpent = await _context.Orders
                    .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                    .SumAsync(o => o.TotalAmount) ?? 0;

                var completedOrders = await _context.Orders
                    .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                    .CountAsync();

                var benefit = newRank switch
                {
                    "Bạc" => "Mã giảm giá 5% tự động áp dụng cho mọi đơn hàng.",
                    "Vàng" => "Mã giảm giá 10% tự động áp dụng cho mọi đơn hàng.",
                    _ => "Hạng khởi đầu với các ưu đãi đặc biệt."
                };

                var emailBody = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
        <tr>
          <td style='padding-bottom: 10px;'>
            <h2 style='color:#FECC29;'>Chúc mừng bạn đã đạt hạng thành viên {newRank}!</h2>
          </td>
        </tr>
        <tr>
          <td style='color:#333333; font-size:16px; line-height:1.6;'>
            <p>Xin chào <strong>{user.FullName}</strong>,</p>
            <p>Chúc mừng bạn đã được thăng hạng thành viên <strong>{newRank}</strong> tại PouletLG!</p>
            <p><strong>Thống kê của bạn:</strong></p>
            <ul>
                <li>Tổng số đơn hàng hoàn thành: <strong>{completedOrders}</strong></li>
                <li>Tổng tiền tích lũy: <strong>{totalSpent:N0} VNĐ</strong></li>
            </ul>
            <p><strong>Lợi ích của hạng {newRank}:</strong></p>
            <ul>
                <li>{benefit}</li>
            </ul>
            <p>Hãy tiếp tục mua sắm để tận hưởng những ưu đãi tuyệt vời hơn nữa!</p>
            <p><a href='https://yourwebsite.com/products' style='color:#FECC29; font-weight:bold;'>Mua sắm ngay</a></p>
            <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";

                await _emailService.SendEmailAsync(user.Email, $"Chúc mừng bạn đạt hạng thành viên {newRank} - PouletLG", emailBody);
                _logger.LogInformation($"Promotion email sent to {user.Email} for UserId={userId}, NewRank={newRank}, TotalSpent={totalSpent:N0}, CompletedOrders={completedOrders}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send promotion email for UserId={userId}, NewRank={newRank}: {ex.Message}");
            }
        }

        // Gửi email khuyến khích khi gần đạt hạng tiếp theo
        private async Task SendMilestoneEmailAsync(int userId, string nextRank, decimal requiredAmount, decimal totalSpent)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for UserId={userId}");
                    return;
                }

                var completedOrders = await _context.Orders
                    .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                    .CountAsync();

                var amountNeeded = requiredAmount - totalSpent;
                var benefit = nextRank switch
                {
                    "Bạc" => "Mã giảm giá 5% tự động áp dụng cho mọi đơn hàng.",
                    "Vàng" => "Mã giảm giá 10% tự động áp dụng cho mọi đơn hàng.",
                    _ => ""
                };

                var emailBody = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
        <tr>
          <td style='padding-bottom: 10px;'>
            <h2 style='color:#FECC29;'>Bạn sắp đạt hạng thành viên {nextRank}!</h2>
          </td>
        </tr>
        <tr>
          <td style='color:#333333; font-size:16px; line-height:1.6;'>
            <p>Xin chào <strong>{user.FullName}</strong>,</p>
            <p>Bạn chỉ còn cần tích lũy thêm <strong>{amountNeeded:N0} VNĐ</strong> để đạt hạng thành viên <strong>{nextRank}</strong> tại PouletLG!</p>
            <p><strong>Thống kê của bạn:</strong></p>
            <ul>
                <li>Tổng số đơn hàng hoàn thành: <strong>{completedOrders}</strong></li>
                <li>Tổng tiền tích lũy: <strong>{totalSpent:N0} VNĐ</strong></li>
            </ul>
            <p><strong>Lợi ích của hạng {nextRank}:</strong></p>
            <ul>
                <li>{benefit}</li>
            </ul>
            <p>Hãy nhanh tay mua sắm để nhận ngay đặc quyền này!</p>
            <p><a href='https://yourwebsite.com/products' style='color:#FECC29; font-weight:bold;'>Mua sắm ngay</a></p>
            <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";

                await _emailService.SendEmailAsync(user.Email, $"Sắp đạt hạng thành viên {nextRank} - PouletLG", emailBody);
                _logger.LogInformation($"Milestone email sent to {user.Email} for UserId={userId}, NextRank={nextRank}, AmountNeeded={amountNeeded:N0}, TotalSpent={totalSpent:N0}, CompletedOrders={completedOrders}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send milestone email for UserId={userId}, NextRank={nextRank}: {ex.Message}");
            }
        }
    }
}