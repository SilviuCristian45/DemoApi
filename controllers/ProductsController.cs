using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- Import obligatoriu
using DemoApi.Utils;

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
    // Simulăm o bază de date temporară (statică) doar ca să testăm
    private static readonly List<string> Products = new List<string> 
    { 
        "Laptop", 
        "Mouse", 
        "Monitor" 
    };

    // 3. GET: api/products
    [HttpGet] // = @Get()
    public ActionResult<ApiResponse<List<string>>> GetAll()
    {
        var response = ApiResponse<List<string>>.Success(Products, "Am găsit toate produsele");
        
        return Ok(response);
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
    public ActionResult<ApiResponse<string>> Create([FromBody] string newProduct) 
    {
        Products.Add(newProduct);
        return Ok(ApiResponse<string>.Success(newProduct)); // 201 Created
    }
}