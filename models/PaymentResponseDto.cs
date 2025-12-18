namespace DemoApi.Models;

public record PaymentResponseDto(
    string PaymentIntentId, 
    string ClientSecret, 
    decimal Amount, 
    string Currency
);