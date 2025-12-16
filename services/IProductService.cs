using DemoApi.Models.Entities; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;
using DemoApi.Models;

namespace DemoApi.Services;

public interface IProductService
{
    Task<ApiResponse<string>> Create(Product product);
    Task<List<GetProductsResponse>> GetAll(PaginatedQueryDto paginatedQueryDto);

    Task<Product?> Update(int id, UpdateProductDto updateProductDto);
    Task<Boolean> IsProductExisting(int id);

    Task<Product?> GetProductById(int id);

    Task<Boolean> DeleteImageIfExisting(string image);

    Task<int> Total(PaginatedQueryDto paginatedQueryDto);
}