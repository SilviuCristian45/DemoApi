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
    
}