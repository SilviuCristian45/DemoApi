using System.Text.Json.Serialization; // Pt JsonPropertyName

namespace DemoApi.Models;

// "Positional Record" - Definește proprietățile și constructorul într-o singură linie
public record GetProductsResponse(int Id, string Name, decimal Price, string CategoryName);