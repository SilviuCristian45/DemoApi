namespace DemoApi.Services;

using DemoApi.Utils;
using DemoApi.Models.Entities;
using DemoApi.Data;
using DemoApi.Models;

using Microsoft.EntityFrameworkCore; // Pt ToListAsync

class ProductService: IProductService
{

    private readonly AppDbContext _context;
    private readonly ILogger<ProductService> _logger;

    // Injectăm Baza de Date AICI, nu în Controller
    public ProductService(AppDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger  = logger;
    }


    public async Task<ApiResponse<string>> Create(Product product) {

        var response = _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return ApiResponse<string>.Success(product.Name);
    }

    public async Task<List<GetProductsResponse>> GetAll(PaginatedQueryDto paginatedQueryDto) {
        var response = await _context.Products
        .Where(p => p.Name.Contains(paginatedQueryDto.Search) || (p.Category != null ? p.Category.Name : "Fara categorie").Contains(paginatedQueryDto.Search) )
        .Skip((paginatedQueryDto.PageNumber - 1) * paginatedQueryDto.PageSize)
        .Take(paginatedQueryDto.PageSize)
        .Select(p => new GetProductsResponse(
            p.Id, 
            p.Name, 
            p.Price,
            p.Category != null ? p.Category.Name : "Fără Categorie"
        ))
        .ToListAsync();
        return response ?? new List<GetProductsResponse>();
    }

    public async Task<Product?> Update(int id, UpdateProductDto updateProductDto) {
        Product? product = await _context.Products.FindAsync(id);

        if (product == null) {
            return null;
        } 

        if (updateProductDto.Name != null) {
            product.Name = updateProductDto.Name;
        }

        if (updateProductDto.Price != null) {
            product.Price = (decimal) updateProductDto.Price;
        }

        if (updateProductDto.CategoryId != null) {
            product.CategoryId = updateProductDto.CategoryId;
        }

        if (updateProductDto.Image != null) {
            product.Image = updateProductDto.Image;
        }

        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Boolean> IsProductExisting(int id) {
        Product? product = await _context.Products.FindAsync(id);
        return product != null;
    }

    public async Task<Boolean> DeleteImageIfExisting(string image) {
        if (string.IsNullOrEmpty(image)) return false;

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", image);

        if (File.Exists(filePath) == false) {
            return false;
        }

        try {
            File.Delete(filePath);
            return true;
        } catch(Exception exception) {
            _logger.LogWarning(exception, exception.ToString());
            return false;
        }
    }

    public async Task<Product?> GetProductById(int id) {
        return await _context.Products.FindAsync(id);
    }

    public async Task<int> Total(PaginatedQueryDto paginatedQueryDto) {
        return await _context.Products.CountAsync(p => p.Name.Contains(paginatedQueryDto.Search) || (p.Category != null ? p.Category.Name : "Fara categorie").Contains(paginatedQueryDto.Search));
    }
    

    
}