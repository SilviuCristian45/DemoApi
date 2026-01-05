using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- Import obligatoriu
using DemoApi.Utils;
using DemoApi.Services;
using DemoApi.Models;
using DemoApi.Models.Entities;
using System.Security.Claims; // <--- Nu uita asta!
using Microsoft.Extensions.Caching.Memory;
using DemoApi.Data;

namespace DemoApi.Controllers;

// 1. [ApiController] = @Controller() din NestJS
// Îi spune framework-ului că asta e o clasă de API (validează automat body-ul, etc.)
[ApiController]
[Route("api/[controller]")]
public class OrdersControllers: ControllerBase
{
    private IOrderService _orderService;
    private readonly ILogger<OrdersControllers> _logger;
    private readonly IMemoryCache _cache;

    public OrdersControllers(
        IOrderService orderService,
        ILogger<OrdersControllers> logger,
        IMemoryCache cache
    ) {
        _orderService = orderService;
        _logger = logger;
        _cache = cache;
    }

    [HttpGet] // = @Get()
    [Authorize(Roles = nameof(Role.ADMIN))]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<OrderResponse>>>> GetAll([FromQuery] PaginatedQueryDto paginatedQueryDto)
    {
        var cacheProductsKey = $"get_products_${paginatedQueryDto.PageNumber}_${paginatedQueryDto.PageSize}_${paginatedQueryDto.Search}";
        var totalProductsKey = $"total_${cacheProductsKey}";
        if (!_cache.TryGetValue(cacheProductsKey, out List<OrderResponse> productsCache))
        {
            _logger.LogInformation("Citim din baza de date");
            var products = await _orderService.GetAll(paginatedQueryDto);

            if (products.Success == false) {
                return BadRequest(products.ErrorMessage);
            }

            var totalProducts = await _orderService.Total(paginatedQueryDto);

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // Expiră în 5 min
                .SetSlidingExpiration(TimeSpan.FromMinutes(2)); // Sau dacă nu e accesat 2 min

            _cache.Set(cacheProductsKey, products.Data, cacheOptions);
            _cache.Set(totalProductsKey, totalProducts, cacheOptions);

            return Ok(ApiResponse<PaginatedResponse<OrderResponse>>.Success( 
                new PaginatedResponse<OrderResponse>
                    (totalProducts,
                    products.Data) ) );
        }
        else
        {
             _logger.LogWarning("Citesc din Cache (RAM)..."); // Pt debug
        }

        _cache.TryGetValue(totalProductsKey, out int totalProductsCache);
        
        return Ok(ApiResponse<PaginatedResponse<OrderResponse>>.Success( 
                new PaginatedResponse<OrderResponse>
                    (totalProductsCache,
                    productsCache ?? new List<OrderResponse>()) ) );
        
    }
}