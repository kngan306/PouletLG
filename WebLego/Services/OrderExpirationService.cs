using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace WebLego.Services
{
    public class OrderExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderExpirationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<DbpouletLgv5Context>();

                    var pendingOrders = await _context.Orders
                        .Where(o => o.OrderStatus == "Chờ thanh toán" && o.OrderDate.HasValue && o.OrderDate.Value.AddHours(24) < DateTime.Now)
                        .ToListAsync();

                    if (pendingOrders.Any())
                    {
                        foreach (var order in pendingOrders)
                        {
                            Console.WriteLine($"Canceling order {order.OrderId} due to expiration");
                            order.OrderStatus = "Đã hủy";
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}