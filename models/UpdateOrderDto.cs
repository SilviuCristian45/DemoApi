namespace DemoApi.Data;

using DemoApi.Models;
using DemoApi.Utils;

public record UpdateOrderRequest (
    string? PhoneNumber,
    string? PaymentType,
    AddressRequest? Address,
    List<CartItem>? Items,
    string? OrderStatus
);