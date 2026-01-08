using Microsoft.AspNetCore.Mvc;
using Stripe;
using DemoApi.Data; // sau namespace-ul contextului tau
using DemoApi.Models.Entities; // namespace-ul entitatii Order
using Microsoft.EntityFrameworkCore;


using DemoApi.Services;
using DemoApi.Utils;

[Route("api/webhook")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly IEmailService _emailService;

    public WebhookController(
        ILogger<WebhookController> logger, 
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IEmailService emailService
    )
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var endpointSecret = _configuration["Stripe:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );

            // 2. Gestionăm doar evenimentul de plată reușită
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                // Stripe ne dă obiectul PaymentIntent
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                
                _logger.LogInformation($"Webhook primit: Plată reușită pentru {paymentIntent?.Id}");
                // 3. Actualizăm baza de date
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var order = await context.Orders.Include(o => o.orderItems).ThenInclude(p => p.Product).FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);
                if (order == null) {
                    _logger.LogError("no order with payment id : " + paymentIntent.Id);
                    return BadRequest();
                }
                await UpdateOrderToPaid(order);
                await context.SaveChangesAsync();
                 _logger.LogInformation($"Comanda {order.Id} a fost actualizată la statusul PAID.");
                await _emailService.SendOrderConfirmationAsync(order?.Email ?? "", order);
            }
            return Ok(); // Confirmăm primirea către Stripe
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Eroare la validarea webhook-ului Stripe");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eroare internă la procesarea webhook-ului");
            return StatusCode(500);
        }
    }

    

    // Metodă privată pentru update DB
    private async Task UpdateOrderToPaid(Order order)
    {
        // Fiind într-un proces async declanșat extern, e safer să creăm un scope nou
        if (order != null)
        {
            // Verificăm să nu fie deja plătită (idempotency)
            if (order.Status != OrderStatus.Accepted) // sau Paid
            {
                order.Status = OrderStatus.Accepted;
            }
        }
        else
        {
            _logger.LogError($"Comanda pentru intent-ul {order.PaymentIntentId} nu a fost găsită în DB!");
        }
    }
}