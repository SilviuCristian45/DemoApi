namespace DemoApi.Services;

using DemoApi.Utils;
using DemoApi.Models.Entities;
using DemoApi.Data;

class ProductService: IProductService
{

    private readonly AppDbContext _context;

    // Injectăm Baza de Date AICI, nu în Controller
    public ProductService(AppDbContext context)
    {
        _context = context;
    }


    public async Task<ApiResponse<string>> Create(Product product) {

        var response = _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return ApiResponse<string>.Success(product.Name);
    }
}