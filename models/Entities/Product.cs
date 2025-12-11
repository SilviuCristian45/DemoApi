using System.ComponentModel.DataAnnotations; // Pt [Key], [Required]

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
}