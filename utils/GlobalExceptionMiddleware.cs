using System.Net;
using System.Text.Json;
namespace DemoApi.Utils; // Asigură-te că aici e clasa ta ApiResponse

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Aici lăsăm cererea să treacă mai departe spre Controller
            await _next(context);
        }
        catch (Exception ex)
        {
            // Dacă crapă ceva, ajungem aici
            _logger.LogError(ex, "A apărut o eroare neașteptată.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        // Setăm status code default 500
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Putem personaliza în funcție de tipul erorii
        // De exemplu, dacă e o eroare de validare sau "KeycloakException", putem returna 400 sau 403
        var message = "A apărut o eroare internă. Te rugăm să încerci mai târziu.";
        
        // În development, vrem să vedem eroarea reală pentru debugging
        // În producție, ascundem detaliile
        var errorDetails = exception.Message ?? message; 

        // Folosim wrapper-ul tău ApiResponse
        var response = ApiResponse<string>.Error(errorDetails);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        return context.Response.WriteAsync(json);
    }
}