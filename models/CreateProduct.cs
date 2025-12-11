namespace DemoApi.Models;

public record CreateProductDto(string Name, decimal Price, int CategoryId);