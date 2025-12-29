using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- Import obligatoriu
using DemoApi.Utils;
using DemoApi.Services;
using DemoApi.Models;
using DemoApi.Models.Entities;
using System.Security.Claims; // <--- Nu uita asta!
using Microsoft.Extensions.Caching.Memory;

namespace DemoApi.Controllers;

// 1. [ApiController] = @Controller() din NestJS
// Îi spune framework-ului că asta e o clasă de API (validează automat body-ul, etc.)
[ApiController]

// 2. [Route] = Prefixul rutei. 
// "[controller]" e un placeholder dinamic. Va lua numele clasei ("Products") și va tăia "Controller".
// Ruta finală va fi: /api/products
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    private readonly IImageService _imageService;
    private readonly IMemoryCache _cache;

    public ProductsController(
        IProductService productService, 
        ILogger<ProductsController> logger, 
        IImageService imageService,
        IMemoryCache cache)
    {
        _productService = productService;
        _logger = logger;
        _imageService = imageService;
        _cache = cache;
    }

    private static readonly List<string> Products = new List<string> 
    { 
        "Laptop", 
        "Mouse", 
        "Monitor" 
    };

    // 3. GET: api/products
    [HttpGet] // = @Get()
    public async Task<ActionResult<ApiResponse<PaginatedResponse<GetProductsResponse>>>> GetAll([FromQuery] PaginatedQueryDto paginatedQueryDto)
    {
        var cacheProductsKey = $"get_products_${paginatedQueryDto.PageNumber}_${paginatedQueryDto.PageSize}_${paginatedQueryDto.Search}";
        var totalProductsKey = $"total_${cacheProductsKey}";
        if (!_cache.TryGetValue(cacheProductsKey, out List<GetProductsResponse> productsCache))
        {
            _logger.LogInformation("Citim din baza de date");
            var products = await _productService.GetAll(paginatedQueryDto);
            var totalProducts = await _productService.Total(paginatedQueryDto);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // Expiră în 5 min
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)); // Sau dacă nu e accesat 2 min

            _cache.Set(cacheProductsKey, products, cacheOptions);
            _cache.Set(totalProductsKey, totalProducts, cacheOptions);

            return Ok(ApiResponse<PaginatedResponse<GetProductsResponse>>.Success( 
                new PaginatedResponse<GetProductsResponse>
                    (totalProducts,
                    products) ) );
        }
        else
        {
             _logger.LogWarning("Citesc din Cache (RAM)..."); // Pt debug
        }

        _cache.TryGetValue(totalProductsKey, out int totalProductsCache);
        
        return Ok(ApiResponse<PaginatedResponse<GetProductsResponse>>.Success( 
                new PaginatedResponse<GetProductsResponse>
                    (totalProductsCache,
                    productsCache ?? new List<GetProductsResponse>()) ) );
        
    }

    // 4. GET: api/products/{index}
    [HttpGet("{index}")] // = @Get(':index')
    public ActionResult<ApiResponse<string>> GetOne(int index)
    {
       if (index < 0 || index >= Products.Count)
        {
            // Returnăm un Error Wrapper, dar tot cu status 200 sau 404, depinde de strategia ta.
            // Frontendul se uită la câmpul "Type".
            return NotFound(ApiResponse<string>.Error("Produsul nu a fost găsit"));
        }

        // Aici T este string
        return Ok(ApiResponse<string>.Success(Products[index]));
    }

    // 5. POST: api/products
    [HttpPost] // = @Post()
    // [FromBody] e implicit, nu trebuie scris neapărat
    [Authorize(Roles = nameof(Role.ADMIN))] // <--- 3. DOAR ADMINII pot adăuga produse
    public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] CreateProductDto newProduct) 
    {
        var result = await _productService.Create(new Models.Entities.Product { Name = newProduct.Name, Price = newProduct.Price, CategoryId = newProduct.CategoryId });
      
        return Ok(ApiResponse<string>.Success(newProduct.Name, result.Message));
    }

    [HttpPost("uploadImage/{index}")]
    [Authorize(Roles = nameof(Role.ADMIN))]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage(int index, [FromForm] UploadImageDto uploadImageDto) { 
        var file = uploadImageDto.File;
        try {
            Console.WriteLine(Path.GetExtension(file.FileName));
            Console.WriteLine(file.ContentType);
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName =  Guid.NewGuid().ToString() + extension;
            var fullPath = Path.Combine(folderPath, fileName);

            Product? product = await _productService.GetProductById(index);

            if (product == null) {
                _logger.LogWarning("Url-ul imaginii nu a fost actualizat in db. Nu s-a gasit id-ul ", index);
                return NotFound(ApiResponse<string>.Error($"Produsul {index} nu a fost gasit"));
            } 

            using (var sr = new FileStream(fullPath, FileMode.Create)) {
                await file.CopyToAsync(sr);
            }
    
            var deleteOldImageIfExisting = await _productService.DeleteImageIfExisting(product.Image);

            if (deleteOldImageIfExisting == false) {
                _logger.LogWarning("stergere imagine veche esuata - verificati logurile de mai sus");
            }

            var updateResult = await _productService.Update(index, new Models.Entities.UpdateProductDto {Image = fileName});
            var publicUrl = await _imageService.uploadImage(file);

            if (publicUrl == null) {
                _logger.LogError("Supabase image upload failed");
                _logger.LogInformation("Imagine uploadată cu succes pe local : {  } si url salvat in db", fullPath);
            }
            return Ok(ApiResponse<string>.Success(publicUrl ?? fullPath));

        } catch(Exception e) {
            _logger.LogError("eroare upload imagine ");
            _logger.LogError(e, e.ToString());
            return BadRequest(ApiResponse<string>.Error("eroare procesare fisier"));
        }
    }


    [HttpPost("orders")]
    [Authorize(Roles = nameof(Role.USER))]
    public async Task<ActionResult<ApiResponse<string>>> PlaceOrder([FromBody] PlaceOrderRequest placeOrderRequest) 
    {
        var keycloakId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
        if (string.IsNullOrEmpty(keycloakId))
        {
            return Unauthorized(ApiResponse<string>.Error("Utilizatorul nu a putut fi identificat."));
        }

        Boolean orderPlacedSuccess = await _productService.PlaceOrder(placeOrderRequest, keycloakId);
        if (orderPlacedSuccess == false) {
            return BadRequest(ApiResponse<string>.Error("Stoc epuizat produs sau o eroare interna a serverului"));
        }
        return (orderPlacedSuccess) ? Ok(ApiResponse<string>.Success("place order")) : BadRequest(ApiResponse<string>.Error("comanda nu s-a putut efectua"));
    }
}