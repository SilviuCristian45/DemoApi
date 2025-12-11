using DemoApi.Models.Entities; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;
using DemoApi.Models;

namespace DemoApi.Services;

public interface IProductService
{
    Task<ApiResponse<string>> Create(Product product);
    Task<ApiResponse<List<GetProductsResponse>>> GetAll();
}