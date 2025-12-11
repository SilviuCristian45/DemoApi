using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Pt JsonIgnore

namespace DemoApi.Models.Entities;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore] 
    public List<Product> Products { get; set; } = new();
}