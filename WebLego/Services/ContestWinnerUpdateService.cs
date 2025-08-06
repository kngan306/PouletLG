using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebLego.DataSet.GdrService;
using WebLego.Services;

namespace WebLego.Services
{
    public class ContestWinnerUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public ContestWinnerUpdateService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DbpouletLgv5Context>();
                        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                        var currentDate = DateTime.Now;
                        var endedContests = await context.Contests
                            .Where(c => c.EndDate < currentDate && c.IsActive && c.ContestStatus != "Đã kết thúc")
                            .ToListAsync(stoppingToken);

                        foreach (var contest in endedContests)
                        {
                            contest.ContestStatus = "Đã kết thúc";
                            await context.SaveChangesAsync(stoppingToken);

                            var winnerPost = await context.CommunityPosts
                                .Include(cp => cp.User)
                                .Include(cp => cp.ContestVotes)
                                .Where(cp => cp.ContestId == contest.ContestId)
                                .OrderByDescending(cp => cp.ContestVotes.Count)
                                .FirstOrDefaultAsync(stoppingToken);

                            if (winnerPost != null && winnerPost.UserId != 0)
                            {
                                var existingWinner = await context.ContestWinners
                                    .AnyAsync(cw => cw.ContestId == contest.ContestId, stoppingToken);

                                if (!existingWinner)
                                {
                                    var winner = new ContestWinner
                                    {
                                        ContestId = contest.ContestId,
                                        UserId = winnerPost.UserId,
                                        RewardProductId = (int)contest.RewardProductId,
                                        WonAt = DateTime.Now,
                                        Status = "Chưa gửi"
                                    };
                                    context.ContestWinners.Add(winner);
                                    await context.SaveChangesAsync(stoppingToken);

                                    // Gửi email thông báo chiến thắng
                                    var winnerUser = await context.Users
                                        .FirstOrDefaultAsync(u => u.UserId == winnerPost.UserId, stoppingToken);
                                    var product = await context.Products
                                        .FirstOrDefaultAsync(p => p.ProductId == contest.RewardProductId, stoppingToken);

                                    if (winnerUser != null)
                                    {
                                        var emailBody = $@"
                                        <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
                                          <tr>
                                            <td align='center'>
                                              <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                                                <tr>
                                                  <td style='padding-bottom: 10px;'>
                                                    <h2 style='color:#FECC29;'>Chúc mừng bạn đã chiến thắng cuộc thi LEGO!</h2>
                                                  </td>
                                                </tr>
                                                <tr>
                                                  <td style='color:#333333; font-size:16px; line-height:1.6;'>
                                                    <p>Xin chào <strong>{winnerUser.FullName}</strong>,</p>
                                                    <p>Chúng tôi rất vui mừng thông báo bạn đã giành chiến thắng trong <strong>{contest.Title}</strong>!</p>
                                                    <p><strong>Chi tiết chiến thắng:</strong></p>
                                                    <ul>
                                                      <li><strong>Tên cuộc thi:</strong> {contest.Title}</li>
                                                      <li><strong>Mô tả:</strong> {contest.Description}</li>
                                                      <li><strong>Bài đăng của bạn:</strong> {winnerPost.Contest}</li>
                                                      <li><strong>Số lượt bình chọn:</strong> {winnerPost.ContestVotes.Count}</li>
                                                      <li><strong>Phần thưởng:</strong> {product?.ProductName ?? "Không xác định"}</li>
                                                    </ul>
                                                    <p>Chúng tôi sẽ sớm tạo đơn hàng phần thưởng và gửi sản phẩm đến địa chỉ của bạn. Bạn chỉ cần thanh toán phí vận chuyển 30,000₫ khi nhận hàng.</p>
                                                    <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                                                  </td>
                                                </tr>
                                              </table>
                                            </td>
                                          </tr>
                                        </table>";

                                        try
                                        {
                                            await emailService.SendEmailAsync(winnerUser.Email, $"Chúc mừng bạn chiến thắng cuộc thi {contest.Title}", emailBody);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Failed to send winner email to {winnerUser.Email}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ContestWinnerUpdateService error: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
                }

                // Chờ 1 giờ trước khi kiểm tra lại
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}