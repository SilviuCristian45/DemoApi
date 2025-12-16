using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- Import obligatoriu
using DemoApi.Utils;
using DemoApi.Services;
using DemoApi.Models;

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

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    private static readonly List<string> Products = new List<string> 
    { 
        "Laptop", 
        "Mouse", 
        "Monitor" 
    };

    // 3. GET: api/products
    [HttpGet] // = @Get()
    public async Task<ActionResult<ApiResponse<List<GetProductsResponse>>>> GetAll()
    {
        var products = await _productService.GetAll();
        return Ok(products);
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

            Boolean isProductExisting = await _productService.IsProductExisting(index);

            if (isProductExisting == false) {
                _logger.LogWarning("Url-ul imaginii nu a fost actualizat in db. Nu s-a gasit id-ul ", index);
                return NotFound(ApiResponse<string>.Error($"Produsul {index} nu a fost gasit"));
            } 

            using (var sr = new FileStream(fullPath, FileMode.Create)) {
                await file.CopyToAsync(sr);
            }

            var updateResult = await _productService.Update(index, new Models.Entities.UpdateProductDto {Image = fileName});

            _logger.LogInformation("Imagine uploadată cu succes: {NumeFisier} si url salvat in db", fullPath);

            return Ok(ApiResponse<string>.Success(fileName));

        } catch(Exception e) {
            _logger.LogError("eroare upload imagine ");
            _logger.LogError(e, e.ToString());
            return BadRequest(ApiResponse<string>.Error("eroare procesare fisier"));
        }
       
        //return Ok(ApiResponse<string>.Success($"salutare . returnam imagine pentru {index}"));
    }
}