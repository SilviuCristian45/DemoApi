namespace DemoApi.Services;
using DemoApi.Models.Entities;

public interface IEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, Order order);
    Task SendOrderShippedAsync(string toEmail, Order order);
}