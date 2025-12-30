using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DemoApi.Data;
using DemoApi.Models.Entities;

namespace DemoApi.Services;

public class OrderCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCleanupService> _logger;

    public OrderCleanupService(IServiceScopeFactory scopeFactory, ILogger<OrderCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("⏳ Order Cleanup Service a pornit.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredOrders();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Eroare la curățarea comenzilor expirate.");
            }
        }
    }

    private async Task ProcessExpiredOrders()
    {
        using var scope = _scopeFactory.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var expirationLimit = DateTime.UtcNow.AddMinutes(-30);

        var expiredOrders = await context.Orders
            .Include(o => o.orderItems)
            .Where(o => o.Status == Utils.OrderStatus.Pending && o.CreatedAt < expirationLimit)
            .ToListAsync();

        if (expiredOrders.Any())
        {
            _logger.LogInformation($"🧹 Am găsit {expiredOrders.Count} comenzi expirate. Începem anularea...");

            var allExpiredOrdersItemsFlat = expiredOrders.SelectMany(o => o.orderItems).Select(s => (s.ProductId, s.Quantity, s.OrderId) ).ToList();
            var expiredOrdersDictionary = expiredOrders.ToDictionary(p => p.Id);

            if (allExpiredOrdersItemsFlat.Count == 0) {
                foreach (var order in expiredOrdersDictionary) {
                    order.Value.Status = Utils.OrderStatus.Expired;
                }
            }

            var allProductsFromOrderItemsDistinct = allExpiredOrdersItemsFlat.Select(h => h.ProductId).Distinct();

            var allProductsToRollbackStock = await context.Products.Where(p => allProductsFromOrderItemsDistinct.Contains(p.Id)).ToListAsync();

            //for O(1) retrieve
            var allProductsRollbackStockDictionary = allProductsToRollbackStock.ToDictionary(p => p.Id);
            

            foreach (var pereche in allExpiredOrdersItemsFlat) {
                allProductsRollbackStockDictionary.TryGetValue(pereche.ProductId, out Product? product);
                expiredOrdersDictionary.TryGetValue(pereche.OrderId ?? 0, out Order? order);
                if (order != null){
                    order.Status = Utils.OrderStatus.Expired;
                }
                if (product != null)
                {
                    product.Stock += pereche.Quantity;
                    _logger.LogInformation($"🔄 Stoc returnat pentru produsul {product.Name} (+{pereche.Quantity} buc).");
                }
            }

            await context.SaveChangesAsync();


            //varianta mai ineficienta (pt fiecare expired order sa ai inca un call la DB e ceva nasol N * (nr ms pt call la db))
            // foreach (var order in expiredOrders)
            // {
            //     // Schimbăm statusul
            //     order.Status = Utils.OrderStatus.Expired; // Sau "Expired"
            //     var productsIds = order.orderItems.Select(p => p.ProductId);
            //     var products = await context.Products.Where( p => productsIds.Contains(p.Id)).ToListAsync();
            //     var productsMap = products.ToDictionary(p => p.Id);
            //     // 4. Returnăm stocul!
            //     foreach (var item in order.orderItems)
            //     {
            //         productsMap.TryGetValue(item.ProductId, out Product? product);
                    
            //         if (product != null)
            //         {
            //             product.Stock += item.Quantity;
            //             _logger.LogInformation($"🔄 Stoc returnat pentru produsul {product.Name} (+{item.Quantity} buc).");
            //         }
            //     }
            // }
            // await context.SaveChangesAsync();
            _logger.LogInformation("✅ Curățare finalizată.");
        }
    }
}