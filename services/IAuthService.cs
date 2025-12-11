using DemoApi.Models; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;

namespace DemoApi.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
}