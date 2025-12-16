namespace DemoApi.Models.Entities;
using System.ComponentModel.DataAnnotations;

public class UpdateProductDto
{
    // Id-ul nu îl punem aici de obicei, pentru că vine din URL (/api/products/5)
    // Dar dacă vrei să îl pui, trebuie să fie nullable
    public int? Id { get; set; }

    // String este reference type, dar punem '?' ca să fim expliciți
    // Scoatem [Required] pentru că la update e opțional
    [MaxLength(100)] 
    public string? Name { get; set; }

    public string? Image { get; set; }

    // Decimal este value type. Fără '?' ar fi automat 0.
    // Cu '?', dacă nu e trimis, este null.
    public decimal? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CategoryId { get; set; }

    // Proprietățile de navigare (Category) nu se pun de obicei în Update DTO
    // Se actualizează doar CategoryId-ul.
}