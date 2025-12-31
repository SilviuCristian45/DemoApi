using System.ComponentModel.DataAnnotations; // Pt [Key], [Required]
using System.ComponentModel.DataAnnotations.Schema; // Pt ForeignKey
using System.Text.Json.Serialization; // Pt JsonIgnore
using DemoApi.Utils;
namespace DemoApi.Models.Entities;

public class Order
{
    [Key] // Spune explicit că asta e cheia primară (PK)
    public int Id { get; set; }

    public decimal Price { get; set; }

    // Putem adăuga un câmp de dată creată automat
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? userId { get; set; } 

    [JsonIgnore] 
    public List<OrderItem> orderItems { get; set; } = new();

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? PaymentIntentId { get; set; }

    public string? Email { get; set; }
    public Address? Address {get; set; }

    public string? PaymentType { get; set; }
}