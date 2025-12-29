public record UserResponse {
    public string Id { get; init; }
    public string Email { get; init; }

    public string Username { get; init; }

    public List<string> Roles { get; init; }
}