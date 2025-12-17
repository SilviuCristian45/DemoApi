using System.ComponentModel.DataAnnotations; // Pt [Key], [Required]
using System.ComponentModel.DataAnnotations.Schema; // Pt ForeignKey

namespace DemoApi.Models.Entities;

public class OrderItem
{
    [Key] // Spune explicit că asta e cheia primară (PK)
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public int? OrderId { get; set; } 

    // 2. Proprietatea de Navigare (Obiectul C#)
    // Asta te ajută să scrii: product.Category.Name
    [ForeignKey("OrderId")] // Leagă proprietatea de ID-ul de mai sus
    public Order? Order { get; set; }

}