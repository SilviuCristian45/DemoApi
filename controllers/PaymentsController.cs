using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // <--- Import obligatoriu
using DemoApi.Utils;
using DemoApi.Services;
using DemoApi.Models;
using DemoApi.Models.Entities;
using System.Security.Claims; // <--- Nu uita asta!


[ApiController]
// 2. [Route] = Prefixul rutei. 
// "[controller]" e un placeholder dinamic. Va lua numele clasei ("Products") și va tăia "Controller".
// Ruta finală va fi: /api/products
[Route("api/[controller]")]
public class PaymentsController: ControllerBase {

    private ILogger<PaymentsController> _logger;
    private IOrderService _orderService;

    public PaymentsController(ILogger<PaymentsController> logger, IOrderService orderService) {
        _logger = logger;
        _orderService = orderService;
    }

    [HttpPost("{orderId}")] // = @Post()
    // [FromBody] e implicit, nu trebuie scris neapărat
    [Authorize(Roles = $"{nameof(Role.USER)},{nameof(Role.ADMIN)}")]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> Create(int orderId) 
    {
        string? keycloakId = User.FindFirstValue(ClaimTypes.NameIdentifier);    
        if (string.IsNullOrEmpty(keycloakId))
        {
            return Unauthorized(ApiResponse<string>.Error("Utilizatorul nu a putut fi identificat."));
        }

        string? userRoleAsString = User.FindFirstValue(ClaimTypes.Role);
       
        if (string.IsNullOrEmpty(userRoleAsString)) {
            return Unauthorized(ApiResponse<string>.Error("Utilizatorul nu are rol setat"));
        }

        Role userRole;
        Boolean userRoleParseSuccess = Enum.TryParse<Role>(userRoleAsString, out userRole);

        if (userRoleParseSuccess == false) {
            return BadRequest($"Given role is : {userRoleAsString} . Roles need to be ${Role.ADMIN} or {Role.DEV} or {Role.USER} or {Role.SUPER_ADMIN} or {Role.GUEST} ");
        }

        ServiceResult<PaymentResponseDto> result = await _orderService.CreatePaymentIntentAsync(orderId, keycloakId, userRole);
        
        if (result.Success == false || result.Data == null)
            return BadRequest("A aparut o eroare la plata comenzii " + result.ErrorMessage);

        return Ok(ApiResponse<PaymentResponseDto>.Success(result.Data));
    }


    [HttpGet()] // = @Post()
    // [FromBody] e implicit, nu trebuie scris neapărat
    [Authorize(Roles = $"{nameof(Role.USER)},{nameof(Role.ADMIN)}")]
     public ActionResult<ApiResponse<string>> get() {
        List<string> userRoleAsString = User.FindAll(ClaimTypes.Role).Select(p => p.Value).ToList();
        string userRole = userRoleAsString.First(p => {
            Role userRole;
            Boolean userRoleParseSuccess = Enum.TryParse<Role>(p, out userRole);
            return userRoleParseSuccess;
        });
        Role mainUserRole;
        Boolean userRoleParseSuccess = Enum.TryParse<Role>(userRole, out mainUserRole);
        if (userRoleParseSuccess) {
            return ApiResponse<string>.Success(mainUserRole.ToString());
        } else {
            return ApiResponse<string>.Success("nimic");
        }
     } 
}