using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;

namespace WebLego.Services
{
    public class ResetRankService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ResetRankService> _logger;

        public ResetRankService(IServiceProvider serviceProvider, ILogger<ResetRankService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ResetRankService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now.Hour == 0 && now.Minute == 0) // Kiểm tra lúc 00:00 mỗi ngày
                {
                    if (now.Day == 1 && now.Month == 1) // Reset vào 01/01
                    {
                        _logger.LogInformation($"Starting rank reset at {now:yyyy-MM-dd HH:mm:ss}");
                        try
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<DbpouletLgv5Context>();
                                var customers = context.CustomerProfiles
                                    .Join(context.Users,
                                          cp => cp.CustomerId,
                                          u => u.UserId,
                                          (cp, u) => new { cp, u })
                                    .Where(x => x.u.Role.ToString() == "Customer")
                                    .Select(x => x.cp);

                                int count = 0;
                                foreach (var customer in customers)
                                {
                                    customer.CustomerRank = "Đồng";
                                    customer.DiscountCode = null;
                                    count++;
                                }

                                await context.SaveChangesAsync();
                                _logger.LogInformation($"Successfully reset {count} customer ranks at {now:yyyy-MM-dd HH:mm:ss}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error resetting ranks at {now:yyyy-MM-dd HH:mm:ss}");
                        }
                    }
                    // Chờ đến 00:00 ngày tiếp theo
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                else
                {
                    // Tính thời gian đến 00:00 ngày tiếp theo
                    var nextMidnight = now.Date.AddDays(1);
                    var delay = (int)(nextMidnight - now).TotalMilliseconds;
                    _logger.LogDebug($"Waiting until {nextMidnight:yyyy-MM-dd HH:mm:ss} for next check.");
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
    }
}