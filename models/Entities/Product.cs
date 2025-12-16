using System.ComponentModel.DataAnnotations; // Pt [Key], [Required]
using System.ComponentModel.DataAnnotations.Schema; // Pt ForeignKey

namespace DemoApi.Models.Entities;

public class Product
{
    [Key] // Spune explicit că asta e cheia primară (PK)
    public int Id { get; set; }

    [Required] // NOT NULL
    [MaxLength(100)] // VARCHAR(100)
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    // Putem adăuga un câmp de dată creată automat
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CategoryId { get; set; } 

    // 2. Proprietatea de Navigare (Obiectul C#)
    // Asta te ajută să scrii: product.Category.Name
    [ForeignKey("CategoryId")] // Leagă proprietatea de ID-ul de mai sus
    public Category? Category { get; set; }

    public string Image { get; set; } = string.Empty;
}