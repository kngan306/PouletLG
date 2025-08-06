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

namespace WebLego.Services
{
    public class MembershipUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MembershipUpdateService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Kiểm tra mỗi 5 phút

        public MembershipUpdateService(IServiceProvider serviceProvider, ILogger<MembershipUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MembershipUpdateService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DbpouletLgv5Context>();
                        var membershipService = scope.ServiceProvider.GetRequiredService<MembershipService>();

                        // Tìm các đơn hàng hoàn thành trong 5 phút gần đây
                        var recentCompletedOrders = await context.Orders
                            .Where(o => o.OrderStatus == "Hoàn thành" && o.OrderDate >= DateTime.Now.AddMinutes(-5))
                            .Select(o => new { o.UserId, o.OrderId })
                            .Distinct()
                            .ToListAsync(stoppingToken);

                        foreach (var order in recentCompletedOrders)
                        {
                            _logger.LogInformation($"Processing membership update for UserId={order.UserId}, OrderId={order.OrderId}");
                            await membershipService.CheckAndUpdateMembershipAsync(order.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in MembershipUpdateService.");
                }

                // Chờ 5 phút trước khi kiểm tra lại
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("MembershipUpdateService stopped.");
        }
    }
}