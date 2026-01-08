namespace DemoApi.Services;

using Stripe;
using DemoApi.Data;
using DemoApi.Models;
using DemoApi.Utils;
using AutoMapper;
using Microsoft.EntityFrameworkCore; // Pt ToListAsync
using AutoMapper.QueryableExtensions; // <--- ASTA LIPSEȘTE

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly PaymentService _paymentService;

    private readonly IMapper _mapper;

    // Injectăm Baza de Date AICI, nu în Controller
    public OrderService(
        AppDbContext context,
        ILogger<OrderService> logger,
        PaymentService paymentService,
        IMapper mapper
    )
    {
        _context = context;
        _logger = logger;
        _paymentService = paymentService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<string>> Update(int orderId, UpdateOrderRequest updateOrderDto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
            .Include(o => o.orderItems)
            .ThenInclude(oi => oi.Product) // <--- CRITIC: Ca să poți modifica stocul direct
            .Include(o => o.Address)
            .FirstOrDefaultAsync(p => p.Id == orderId);

            if (order == null)
            {
                return ServiceResult<string>.Fail($"order with id {orderId} not found");
            }

            if (updateOrderDto.OrderStatus != null)
            {
                bool orderStatusSuccessParse = OrderStatus.TryParse(updateOrderDto.OrderStatus, out OrderStatus orderStatus);
                if (orderStatusSuccessParse)
                {
                    order.Status = orderStatus;
                }
            }

            if (updateOrderDto.PhoneNumber != null)
            {
                order.PhoneNumber = updateOrderDto.PhoneNumber;
            }

            if (updateOrderDto.PaymentType != null)
            {
                order.PaymentType = updateOrderDto.PaymentType;
            }

            if (updateOrderDto.Address != null)
            {
                order.Address.City = updateOrderDto.Address.City;
                order.Address.Street = updateOrderDto.Address.Street;
                order.Address.StreetNumber = updateOrderDto.Address.StreetNumber;
            }

            
            if (updateOrderDto.Items != null && updateOrderDto.Items.Count > 0)
            {
                foreach (var oldItem in order.orderItems)
                {
                    // Verificăm dacă produsul mai există în DB (să nu fie null)
                    if (oldItem.Product != null)
                    {
                        oldItem.Product.Stock += oldItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                order.orderItems.Clear();
                
                decimal totalPrice = 0;

                var newProductIds = updateOrderDto.Items.Select(x => x.ProductId).Distinct().ToList();
                var productsInDb = await _context.Products
                                             .Where(p => newProductIds.Contains(p.Id))
                                             .ToListAsync();

                foreach (var item in updateOrderDto.Items)
                {
                    var product = productsInDb.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null) {
                        if (product.Stock >= item.Quantity) {
                            product.Stock -= item.Quantity;
                            order.orderItems.Add(new Models.Entities.OrderItem() {
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                OrderId = order.Id,
                                Price = (product?.Price ?? 0) * item.Quantity
                            });
                            totalPrice += (product?.Price ?? 0) * item.Quantity;
                        } else {
                            return ServiceResult<string>.Fail($"Stoc insuficient pentru {product.Name}.");
                        }
                    }
                }

                order.Price = totalPrice;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ServiceResult<string>.Ok("suces");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, ex.Message);
            return ServiceResult<string>.Fail("fail update");
        }
    }

    public async Task<ServiceResult<List<OrderResponse>>> GetAll(PaginatedQueryDto paginatedQueryDto)
    {
        try
        {
            var orders = await _context.Orders
                .Where(p => p.Address.City.Contains(paginatedQueryDto.Search))
                .Skip(paginatedQueryDto.PageNumber)
                .Take(paginatedQueryDto.PageSize)
                .OrderByDescending(p => p.Id)
                .ProjectTo<OrderResponse>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return ServiceResult<List<OrderResponse>>.Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.ToString());
            return ServiceResult<List<OrderResponse>>.Fail(ex.Message);
        }
    }

    public async Task<int> Total(PaginatedQueryDto paginatedQueryDto)
    {
        try
        {
            var total = await _context.Orders
                .Where(p => p.Address.City.Contains(paginatedQueryDto.Search))
                .CountAsync();


            return total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.ToString());
            return 0;
        }
    }

    public async Task<ServiceResult<PaymentResponseDto>> CreatePaymentIntentAsync(int orderId, string userId, Role userRole)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return ServiceResult<PaymentResponseDto>.Fail($"nu a fost gasita comanda cu id {orderId}");
        // 2. Validări de securitate
        // TODO: Verifică dacă order.UserId este același cu user-ul logat!
        // Altfel eu pot plăti comanda ta (sau mai rău, pot vedea detalii despre ea).
        if (order.userId?.Equals(userId) == false && userRole != Role.ADMIN)
        {
            return ServiceResult<PaymentResponseDto>.Fail($"plata nu a fost initiata de userul care a facut comanda");
        }

        if (order.Status == OrderStatus.Accepted)
            return ServiceResult<PaymentResponseDto>.Fail("Comanda este deja plătită.");

        PaymentIntent intent;
        try
        {
            if (string.IsNullOrEmpty(order.PaymentIntentId) == false)
            {
                intent = await _paymentService.GetAsync(order.PaymentIntentId);

                // Verificăm să nu fie deja plătită
                if (intent.Status == "succeeded")
                {
                    return ServiceResult<PaymentResponseDto>.Fail("Această comandă este deja plătită la Stripe.");
                }
            }
            else
            {
                intent = await _paymentService.CreatePaymentIntentAsync(order);

            }
            // 4. Salvăm ID-ul intenției în baza noastră (Opțional, dar recomandat pt debug)
            order.PaymentIntentId = intent.Id;
            await _context.SaveChangesAsync();

            var responseDto = new PaymentResponseDto(
                intent.Id,
                intent.ClientSecret,
                order.Price,
                "ron"
            );
            // 5. Returnăm ClientSecret către Frontend
            return ServiceResult<PaymentResponseDto>.Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.ToString());
            return ServiceResult<PaymentResponseDto>.Fail("Eroare la inițierea plății.");
        }
    }

}