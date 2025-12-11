using DemoApi.Models;
using DemoApi.Utils;

namespace DemoApi.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // Injectăm dependențele exact ca înainte, dar acum în Service
    public AuthService(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;

        // Logica de SSL Bypass (păstrată aici)
        var handler = new HttpClientHandler();
        if (env.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        _httpClient = new HttpClient(handler);
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var tokenUrl = _configuration["Keycloak:TokenUrl"];
        
        var keycloakParams = new Dictionary<string, string>
        {
            {"grant_type", "password"},
            {"client_id", _configuration["Keycloak:ClientId"]!},
            {"client_secret", _configuration["Keycloak:ClientSecret"]!},
            {"username", request.Username},
            {"password", request.Password}
        };

        using var form = new FormUrlEncodedContent(keycloakParams);

        // Call extern
        var response = await _httpClient.PostAsync(tokenUrl, form);

        // 1. Gestionare Eroare
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<LoginResponse>.Error($"Keycloak Error: {errorContent}");
        }

        // 2. Gestionare Succes
        var tokenData = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        if (tokenData == null)
        {
            return ApiResponse<LoginResponse>.Error("Răspuns invalid de la server.");
        }

        return ApiResponse<LoginResponse>.Success(tokenData);
    }
}