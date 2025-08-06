using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebLego.DataSet.GdrService;
using WebLego.Services;
using Microsoft.Extensions.Caching.Memory;

namespace WebLego.Services
{
    public class ContestReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContestReminderService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Kiểm tra mỗi 15 phút
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(2); // Lưu cache 2 ngày

        public ContestReminderService(IServiceProvider serviceProvider, ILogger<ContestReminderService> logger, IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContestReminderService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DbpouletLgv5Context>();
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                        var currentDate = DateTime.Now;
                        var reminderWindowStart = currentDate.AddHours(24 - 0.25); // 24 giờ - 15 phút
                        var reminderWindowEnd = currentDate.AddHours(24 + 0.25); // 24 giờ + 15 phút

                        var contests = await context.Contests
                            .Where(c => c.IsActive && c.ContestStatus == "Đang diễn ra" &&
                                        c.EndDate >= reminderWindowStart && c.EndDate <= reminderWindowEnd)
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation($"Found {contests.Count} contests within 24-hour reminder window at {currentDate:dd/MM/yyyy HH:mm:ss}.");

                        foreach (var contest in contests)
                        {
                            _logger.LogInformation($"Processing contest: ContestId={contest.ContestId}, Title={contest.Title}, EndDate={contest.EndDate:dd/MM/yyyy HH:mm:ss}");

                            // Lấy số lượng người tham gia
                            var totalParticipants = await context.CommunityPosts
                                .CountAsync(cp => cp.ContestId == contest.ContestId, stoppingToken);

                            // Lấy danh sách khách hàng có đơn hàng trong 30 ngày
                            var customers = await context.Users
                                .Where(u => u.RoleId == 1)
                                .Join(context.Orders,
                                    u => u.UserId,
                                    o => o.UserId,
                                    (u, o) => new { u.UserId, u.FullName, u.Email, o.OrderDate })
                                .Where(uo => uo.OrderDate >= currentDate.AddDays(-30))
                                .Select(uo => new { uo.UserId, uo.FullName, uo.Email })
                                .Distinct()
                                .ToListAsync(stoppingToken);

                            _logger.LogInformation($"Found {customers.Count} eligible customers for contest {contest.ContestId}.");

                            foreach (var customer in customers)
                            {
                                var cacheKey = $"ReminderEmail_{contest.ContestId}_{customer.UserId}";
                                if (!_cache.TryGetValue(cacheKey, out DateTime _))
                                {
                                    // Đếm số đơn hàng của khách hàng
                                    var userOrders = await context.Orders
                                        .CountAsync(o => o.UserId == customer.UserId && o.OrderDate >= currentDate.AddDays(-30), stoppingToken);

                                    var emailBody = $@"
<table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
  <tr>
    <td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
        <tr>
          <td style='padding-bottom: 10px;'>
            <h2 style='color:#FECC29;'>Nhắc nhở: Cuộc thi LEGO sắp kết thúc!</h2>
          </td>
        </tr>
        <tr>
          <td style='color:#333333; font-size:16px; line-height:1.6;'>
            <p>Xin chào <strong>{customer.FullName}</strong>,</p>
            <p>Cuộc thi <strong>{contest.Title}</strong> chỉ còn <strong>24 giờ</strong> nữa sẽ kết thúc! Bạn đã tham gia chưa?</p>
            <p><strong>Chi tiết cuộc thi:</strong></p>
            <ul>
              <li><strong>Tên cuộc thi:</strong> {contest.Title}</li>
              <li><strong>Mô tả:</strong> {contest.Description}</li>
              <li><strong>Thời gian kết thúc:</strong> {contest.EndDate:dd/MM/yyyy HH:mm}</li>
              <li><strong>Phần thưởng:</strong> {context.Products.FirstOrDefault(p => p.ProductId == contest.RewardProductId)?.ProductName ?? "Không xác định"}</li>
              <li><strong>Số người tham gia:</strong> {totalParticipants}</li>
            </ul>
            <p><strong>Thống kê của bạn:</strong></p>
            <ul>
              <li><strong>Số đơn hàng trong 30 ngày:</strong> {userOrders}</li>
            </ul>
            <p><strong>Cách tham gia:</strong></p>
            <p>Nếu bạn có <strong>đơn hàng trong 30 ngày gần nhất</strong>, hãy vào đơn hàng và <strong>chia sẻ cảm xúc</strong> về bất kỳ sản phẩm LEGO nào. Bài đăng của bạn sẽ được đưa vào cuộc thi để nhận lượt bình chọn!</p>
            <p><a href='https://yourwebsite.com/contests' style='color:#FECC29; font-weight:bold;'>Tham gia ngay</a></p>
            <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>";

                                    try
                                    {
                                        await emailService.SendEmailAsync(customer.Email, $"Nhắc nhở: Cuộc thi {contest.Title} sắp kết thúc!", emailBody);
                                        _cache.Set(cacheKey, DateTime.Now, _cacheDuration);
                                        _logger.LogInformation($"Reminder email sent to {customer.Email} for ContestId={contest.ContestId}, Title={contest.Title}");
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, $"Failed to send reminder email to {customer.Email} for ContestId={contest.ContestId}: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation($"Reminder email for ContestId={contest.ContestId}, UserId={customer.UserId} already sent, skipping.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"ContestReminderService error: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
                }

                // Chờ 15 phút trước khi kiểm tra lại
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("ContestReminderService stopped.");
        }
    }
}