namespace DemoApi.Services;

using DemoApi.Utils;
using DemoApi.Models.Entities;
using DemoApi.Data;
using DemoApi.Models;

using Microsoft.EntityFrameworkCore; // Pt ToListAsync

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

    public async Task<ApiResponse<List<GetProductsResponse>>> GetAll() {
        var response = await _context.Products.Select(p => new GetProductsResponse(
            p.Id, 
            p.Name, 
            p.Price,
            p.Category != null ? p.Category.Name : "Fără Categorie"
        )).ToListAsync();

        return ApiResponse<List<GetProductsResponse>>.Success(response);
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

    
}