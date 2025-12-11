using Microsoft.AspNetCore.Mvc;
using DemoApi.Models; // Asigură-te că faci using la DTO
using DemoApi.Services;
using DemoApi.Utils;

namespace DemoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    private readonly IAuthService _authService;

    public AuthController(IConfiguration configuration, IWebHostEnvironment env, IAuthService authService)
    {
         _configuration = configuration;
        // 2. Dacă suntem în Development, ignorăm erorile de certificat SSL
        var handler = new HttpClientHandler();
        if (env.IsDevelopment())
        {
            // Această linie spune: "Returnează true (valid) indiferent ce eroare are certificatul"
            handler.ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        _httpClient = new HttpClient(handler); // Simplificare pt moment
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result.Type == Utils.ResponseType.Error)
        {
            return BadRequest(result);
        }
       return Ok(result);
    }
}