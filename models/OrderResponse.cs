namespace DemoApi.Data;
using DemoApi.Utils;
public record OrderResponse {
    public int Id { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string? PaymentIntentId { get; init; }
}