namespace DemoApi.Services;

using DemoApi.Utils;
using DemoApi.Models.Entities;
using DemoApi.Data;
using DemoApi.Models;
using AutoMapper; // <--- Namespace-ul necesar
using AutoMapper.QueryableExtensions; // <--- ASTA LIPSEȘTE

using Microsoft.EntityFrameworkCore; // Pt ToListAsync

class ProductsService: IProductService
{

    private readonly IMapper _mapper; // <--- Field-ul pentru mapper
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsService> _logger;

    // Injectăm Baza de Date AICI, nu în Controller
    public ProductsService(AppDbContext context, ILogger<ProductsService> logger, IMapper mapper)
    {
        _context = context;
        _logger  = logger;
        _mapper = mapper;
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
    
    public async Task<Boolean> PlaceOrder(PlaceOrderRequest placeOrderRequest, string userId, string email) {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var productsIds = placeOrderRequest.Items.Select(item => item.ProductId);
        try {
            decimal totalPrice = 0;
            var newOrder = await _context.Orders.AddAsync(
                new Order() {
                    Price = 0,
                    userId = userId,
                    Email = email
                }
            );
            var products = await _context.Products.Where(p => productsIds.Contains(p.Id)).ToListAsync();
            var productsMap = products.ToDictionary(p => p.Id);
            foreach (var item in placeOrderRequest.Items) {
                productsMap.TryGetValue(item.ProductId, out Product? product);
                if (product == null) {
                    _logger.LogWarning("Produsul cu {produsId} nu a fost gasit in db", item.ProductId);
                    return false;
                }
                if (product.Stock < item.Quantity) {
                    _logger.LogWarning("Produsul {productName} are stocul ${stoc} si s-au cerut ${quantity} bucati. Nu se poate efectua comanda", product.Name, product.Stock, item.Quantity);
                    return false;
                }
                decimal itemCostOnePiece = product.Price;
                decimal cost = item.Quantity * itemCostOnePiece;
                
                product.Stock -= item.Quantity;

                totalPrice += cost;

                newOrder.Entity.orderItems.Add(new OrderItem() {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = product.Price,
                });
            }
            newOrder.Entity.Price = totalPrice;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        } catch(Exception ex) {
            await transaction.RollbackAsync();
            _logger.LogError(ex, ex.ToString());
            return false;
        }
    }

    private async Task<decimal?> computeTotalPrice(PlaceOrderRequest placeOrderRequest) {
        decimal totalPrice = 0;
        List<Product> productsToUpdate = new List<Product>();
        foreach (var item in placeOrderRequest.Items)
        {
            Product? product = await this.GetProductById(item.ProductId);
            if (product == null) {
                _logger.LogWarning("Produsul cu {produsId} nu a fost gasit in db", item.ProductId);
                return null;
            }
            if (product.Stock < item.Quantity) {
                _logger.LogWarning("Produsul {productName} are stocul ${stoc} si s-au cerut ${quantity} bucati. Nu se poate efectua comanda", product.Name, product.Stock, item.Quantity);
                return null;
            }
            decimal itemCostOnePiece = product.Price;
            decimal cost = item.Quantity * itemCostOnePiece;
            
            product.Stock -= item.Quantity;
            productsToUpdate.Append(product);

            totalPrice += cost;
        }

        productsToUpdate.ForEach(product => _context.Products.Update(product));
        await _context.SaveChangesAsync();

        return totalPrice;
    }

    private async Task<int?> createOrder(decimal totalPrice, PlaceOrderRequest placeOrderRequest, string userId) {
        try {
            var newOrder = await _context.Orders.AddAsync(new Order() {
                Price = totalPrice,
                userId = userId,
            });
            await _context.SaveChangesAsync();
            return newOrder.Entity.Id;
        } catch (Exception exception) {
            _logger.LogError(exception, exception.ToString());
            return null;
        }
    }

    private async Task<Boolean> createOrderItems(int orderId, PlaceOrderRequest placeOrderRequest)
    {
        try {
            foreach (var item in placeOrderRequest.Items)
            {
                await _context.OrderItems.AddAsync(new OrderItem() {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity, 
                    OrderId = orderId,
                });
            }
            await _context.SaveChangesAsync();
            return true;
        } catch(Exception ex) {
            _logger.LogError(ex, ex.ToString());
            return false;
        }
        
    }

    public async Task<ServiceResult<List<OrderResponse>>> GetOrders(string userId) {
        var orders = await _context.Orders
            .Where( order => order.userId.Equals(userId) )
            .OrderBy( order => order.CreatedAt)
            .ProjectTo<OrderResponse>(_mapper.ConfigurationProvider) 
            .ToListAsync();   
        return ServiceResult<List<OrderResponse>>.Ok(orders);
    }   
}