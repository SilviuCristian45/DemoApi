using DemoApi.Models.Entities; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;

namespace DemoApi.Services;

public interface IProductService
{
    Task<ApiResponse<string>> Create(Product product);
}