namespace DemoApi.Services;
using DemoApi.Models.Entities;
using MailKit.Net.Smtp;  // Pentru SmtpClient
using MailKit.Security;  // Pentru SecureSocketOptions
using MimeKit;           // Pentru MimeMessage, BodyBuilder

public class EmailService: IEmailService {

    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger,  IConfiguration configuration) {
        _logger = logger;
        _configuration = configuration;
    }
    
    public async Task SendOrderConfirmationAsync(string toEmail, Order order) {
        await this.SendEmailTest(toEmail, order, "order confirmation async");
    }

    public async Task SendOrderShippedAsync(string toEmail, Order order) {
        await this.SendEmailTest(toEmail, order, "order shipped ");
    }

    private async Task SendEmailTest(string toEmail, Order order, string subject)
    {
        if (order == null)
        {
            _logger.LogWarning("nu a fost gasita comanda cu payment Id de mai sus");
            return;
        }

        // 1. Crearea Mesajului (MimeKit)
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("Nume Expeditor", "expeditor@demo.com"));
        email.To.Add(new MailboxAddress("Client", toEmail ?? "silviudinca412@gmail.com"));
        email.Subject = subject;

        var builder = new BodyBuilder();
        builder.HtmlBody = $"<h1>Pret total comanda : {order.Price}</h1>";

        foreach (var item in order.orderItems)
        {
            builder.HtmlBody += "<p>" + item.Product?.Name.ToString() + " " + item.Quantity.ToString() + "</p>"; 
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
}