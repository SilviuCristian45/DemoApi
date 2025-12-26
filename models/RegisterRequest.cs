namespace DemoApi.Models;

public record RegisterRequest {
    public string Username { get; init; }
    public string Password { get; init; }
    public string Email {get; init; }

    public string Role {get; init; }
}