using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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

    public AuthController(IConfiguration configuration, IAuthService authService)
    {
         _configuration = configuration;
        // 2. Dacă suntem în Development, ignorăm erorile de certificat SSL
        _httpClient = new HttpClient(); // Simplificare pt moment
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

    [HttpPost("register")]
    [Authorize(Roles = nameof(Role.ADMIN))]
    public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterRequest request) {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success) {
            return BadRequest( ApiResponse<string>.Error(result.ErrorMessage ?? ""));
        }
        return Ok(ApiResponse<string>.Success(result.Data ?? ""));
    }
}