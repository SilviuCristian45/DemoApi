using Microsoft.AspNetCore.Mvc;
using Stripe;
using DemoApi.Data; // sau namespace-ul contextului tau
using DemoApi.Models.Entities; // namespace-ul entitatii Order
using Microsoft.EntityFrameworkCore;

using MailKit.Net.Smtp;  // Pentru SmtpClient
using MailKit.Security;  // Pentru SecureSocketOptions
using MimeKit;           // Pentru MimeMessage, BodyBuilder

using DemoApi.Utils;

[Route("api/webhook")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public WebhookController(
        ILogger<WebhookController> logger, 
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _scopeFactory = scopeFactory;
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
                await UpdateOrderToPaid(paymentIntent.Id);
                await this.SendEmailTest(paymentIntent.Id);
                
            }
            // Putem gestiona și 'payment_intent.payment_failed' dacă vrem

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

    private async Task SendEmailTest(string paymentIntentId)
    {

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await context.Orders
        .Include(o => o.orderItems)
        .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

        if (order == null)
        {
            _logger.LogWarning("nu a fost gasita comanda cu payment Id " + paymentIntentId);
            return;
        }

        // 1. Crearea Mesajului (MimeKit)
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("Nume Expeditor", "expeditor@demo.com"));
        email.To.Add(new MailboxAddress("Client", "silviudinca412@gmail.com"));
        email.Subject = "Test SMTP din C#";

        var builder = new BodyBuilder();
        builder.HtmlBody = $"<h1>Pret total comanda : {order.Price}</h1>";

        foreach (var item in order.orderItems)
        {
            builder.HtmlBody += "<p>" + item.ProductId.ToString() + " " + item.Quantity.ToString() + "</p>"; 
        }

        email.Body = builder.ToMessageBody();

        // 2. Trimiterea Mesajului (MailKit)
        using var smtp = new SmtpClient(); // Atenție: e SmtpClient din MailKit, nu System.Net!

        try 
        {
            // Conectare la Mailtrap
            // Porturile uzuale Mailtrap: 2525 sau 587
            await smtp.ConnectAsync("sandbox.smtp.mailtrap.io", 2525, SecureSocketOptions.StartTls);

            // Autentificare (User si Pass din Mailtrap Dashboard)
            
            await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);

            // Trimitere
            await smtp.SendAsync(email);
            
            Console.WriteLine("Email trimis cu succes!");
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Eroare: {ex.Message}");
        }
        finally
        {
            // Deconectare curată
            await smtp.DisconnectAsync(true);
        }
    }

    // Metodă privată pentru update DB
    private async Task UpdateOrderToPaid(string paymentIntentId)
    {
        // Fiind într-un proces async declanșat extern, e safer să creăm un scope nou
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await context.Orders.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

        if (order != null)
        {
            // Verificăm să nu fie deja plătită (idempotency)
            if (order.Status != OrderStatus.Accepted) // sau Paid
            {
                order.Status = OrderStatus.Accepted;
                await context.SaveChangesAsync();
                _logger.LogInformation($"Comanda {order.Id} a fost actualizată la statusul PAID.");
            }
        }
        else
        {
            _logger.LogError($"Comanda pentru intent-ul {paymentIntentId} nu a fost găsită în DB!");
        }
    }
}