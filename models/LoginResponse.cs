using System.Text.Json.Serialization; // Pt JsonPropertyName

namespace DemoApi.Models;

public record LoginResponse(
    // Mapăm "access_token" din JSON-ul Keycloak la "AccessToken" din C#
    [property: JsonPropertyName("access_token")] string AccessToken,
    
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    
    [property: JsonPropertyName("token_type")] string TokenType
);