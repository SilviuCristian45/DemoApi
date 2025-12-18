using Stripe;
using DemoApi.Models.Entities; // Asigură-te că ai namespace-ul corect pentru Order

namespace DemoApi.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentIntentService _service;

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
        _service = new PaymentIntentService();
    }

    public async Task<PaymentIntent> GetAsync(string paymentId) {
        return await _service.GetAsync(paymentId);
    }
    public async Task<PaymentIntent> CreatePaymentIntentAsync(Order order)
    {
        var amountInCents = (long)(order.Price * 100);
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = "ron", // Sau "usd", "eur"
            
            Metadata = new Dictionary<string, string>
            {
                { "OrderId", order.Id.ToString() },
                { "UserId", order.userId ?? " Guest User " } // Sau UserEmail dacă îl ai
            },
            
            // Activăm metodele automate de plată (Card, Apple Pay, Google Pay)
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };
        try 
        {
            // Asta face request-ul HTTP către Stripe
            PaymentIntent intent = await _service.CreateAsync(options);
            return intent;
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Eroare la crearea PaymentIntent Stripe");
            throw; // Aruncăm eroarea mai departe să o prindă Controller-ul
        }
    }
}