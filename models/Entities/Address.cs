using System.ComponentModel.DataAnnotations; // Pt [Key], [Required]
using System.ComponentModel.DataAnnotations.Schema; // Pt ForeignKey
using System.Text.Json.Serialization; // Pt JsonIgnore
using DemoApi.Utils;
namespace DemoApi.Models.Entities;

public class Address
{
    [Key] // Spune explicit că asta e cheia primară (PK)
    public int Id { get; set; }

    public string City { get; set; }

    public string Street { get; set; }

    public int StreetNumber { get; set; }

    public int ZipCode { get; set; }

    public int? OrderId {get; set;}
    [ForeignKey("OrderId")] // Leagă proprietatea de ID-ul de mai sus

    public Order? Order {get; set; }

}