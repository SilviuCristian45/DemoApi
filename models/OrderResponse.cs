namespace DemoApi.Data;
using DemoApi.Utils;
public record OrderResponse {
    public int Id { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public string PaymentIntentId { get; init; }

    public List<OrderItemResponse> OrderItems {get; init;}
    
}

public record OrderItemResponse 
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty; // Init cu valoare default ca să nu fie null
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}