namespace DemoApi.Services;

using DemoApi.Data;
using DemoApi.Models;
using DemoApi.Utils;

public class OrderService:  IOrderService {
    private readonly AppDbContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly PaymentService _paymentService;

    // Injectăm Baza de Date AICI, nu în Controller
    public OrderService(
        AppDbContext context, 
        ILogger<OrderService> logger,
        PaymentService paymentService
    )
    {
        _context = context;
        _logger  = logger;
        _paymentService = paymentService;
    }

    public async Task<ServiceResult<PaymentResponseDto>> CreatePaymentIntentAsync(int orderId, string userId, Role userRole)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null) 
            return ServiceResult<PaymentResponseDto>.Fail($"nu a fost gasita comanda cu id {orderId}");
        // 2. Validări de securitate
        // TODO: Verifică dacă order.UserId este același cu user-ul logat!
        // Altfel eu pot plăti comanda ta (sau mai rău, pot vedea detalii despre ea).
        if (order.userId?.Equals(userId) == false) {
            return ServiceResult<PaymentResponseDto>.Fail($"plata nu a fost initiata de userul care a facut comanda");
        }

        if (order.Status == OrderStatus.Accepted)
            return ServiceResult<PaymentResponseDto>.Fail("Comanda este deja plătită.");

        try
        {
            // 3. Creăm intenția prin Stripe
            var intent = await _paymentService.CreatePaymentIntentAsync(order);

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