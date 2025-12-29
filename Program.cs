using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DemoApi.Utils;
using System.Security.Claims; // <--- OBLIGATORIU
using System.Text.Json;       // <--- OBLIGATORIU
using FluentValidation;
using FluentValidation.AspNetCore;
using Stripe;
using Supabase;


using Microsoft.AspNetCore.Mvc; // Pt ApiBehaviorOptions
using DemoApi.Models; // Pt ApiResponse
using DemoApi.Hubs;
using DemoApi.Services;
using DemoApi.Data;

var builder = WebApplication.CreateBuilder(args);
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var keycloakConfig = builder.Configuration.GetSection("Keycloak");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // <--- SCHIMBARE: Citim din config, nu hardcodat
        options.Authority = keycloakConfig["Authority"] ?? "http://localhost:8080/realms/myrealm"; 
        options.Audience = keycloakConfig["ClientId"] ?? "myclient";

        Console.WriteLine(options.Authority);
        Console.WriteLine(options.Audience);
        options.RequireHttpsMetadata = false;
        
        // 3. Dacă .NET nu reușește să ia metadatele automat, le forțăm (opțional, dar util)
        // options.MetadataAddress = $"{keycloak["Authority"]}/.well-known/openid-configuration";
        options.TokenValidationParameters = new TokenValidationParameters
        {
           // Ignorăm cine e destinatarul (aud) - Keycloak pune 'account' uneori
            ValidateAudience = false, 
            
            // Ignorăm cine a emis tokenul (iss) - Docker vs Localhost issues
            ValidateIssuer = false,   
            
            // Verificăm doar: Semnătura (Cheia) și Timpul (să nu fie expirat)
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            
            // Toleranță la ceas (dacă Docker are ora puțin diferită de Windows)
            ClockSkew = TimeSpan.Zero
        };
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                // 1. Căutăm claim-ul "realm_access" (care e un JSON string)
                var realmAccess = context.Principal?.FindFirst("realm_access");
                
                if (realmAccess != null)
                {
                    // 2. Parsăm JSON-ul
                    var element = JsonDocument.Parse(realmAccess.Value).RootElement;
                    Console.WriteLine(element);
                    // 3. Căutăm proprietatea "roles"
                    if (element.TryGetProperty("roles", out var roles))
                    {
                        Console.WriteLine(roles);
                        var claimsIdentity = (ClaimsIdentity)context.Principal!.Identity!;
                        
                        // 4. Luăm fiecare rol din array și îl adăugăm ca un claim de tip .NET Role
                        foreach (var role in roles.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            // Adaugă claim-ul standard pe care îl caută [Authorize]
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName!));
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });



// ... builder.Services.AddControllers();



// ... builder.Services.AddFluentValidationAutoValidation(); ...

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              // Truc pentru Dev: Lăsăm orice origine, dar acceptăm și Credentials
              .SetIsOriginAllowed(origin => true) 
              .AllowCredentials(); // <--- OBLIGATORIU pentru SignalR
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductsService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 1. Configurare Client Supabase
var url = builder.Configuration["Supabase:Url"] ?? "localhost";
var key = builder.Configuration["Supabase:Key"] ?? "1234";

var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
};

// Singleton pentru că vrem o singură conexiune deschisă
var supabaseClient = new Client(url, key, options);
await supabaseClient.InitializeAsync(); // <--- Important: Inițializarea!

builder.Services.AddSingleton(supabaseClient);

builder.Services.AddScoped<IImageService, ImageService>();

builder.Services.AddFluentValidationAutoValidation(); // Activează validarea automată înainte de Controller
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---> BLOCUL DE STANDARDIZARE A ERORILOR DE VALIDARE <---
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Suprascriem fabrica ce generează răspunsul de eroare 400
    options.InvalidModelStateResponseFactory = context =>
    {
        // 1. Extragem toate erorile din ModelState
        // Rezultatul va fi o listă de string-uri: ["Numele este obligatoriu", "Prețul e negativ"]
        Console.WriteLine(context.ModelState);
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .SelectMany(x => x.Value.Errors)
            .Select(x => x.ErrorMessage)
            .ToList();
        
        // 3. Creăm obiectul tău standard ApiResponse
        // (Presupun că ai o metodă .Error() care acceptă și o listă de detalii/erori)
        // Dacă nu ai câmp de List<string> în ApiResponse, poți face string.Join(", ", errors)
        var response = ApiResponse<object?>.Error(errors);

        // 4. Returnăm 400 Bad Request cu formatul NOSTRU
        return new BadRequestObjectResult(response);
    };
});


// ---> ADAUGĂ ACESTE DOUĂ LINII <---
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    // Asta RĂMÂNE (Definiția schemei - butonul mare Authorize de sus)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    // ADAUGĂ linia asta care activează filtrul creat de noi:
    c.OperationFilter<SecurityRequirementsOperationFilter>(); 
});

var app = builder.Build();


// De obicei Swagger e activat doar în Development (ca să nu expui API-ul public)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Generează fișierul .json (ex: /swagger/v1/swagger.json)
    app.UseSwaggerUI(); // Generează interfața grafică HTML (ex: /swagger/index.html)
}

app.MapHub<NotificationsHub>("/hubs/notifications"); // Asta va fi adresa ws://localhost:port/hubs/notifications
// Adaugă linia asta:
app.UseMiddleware<DemoApi.Utils.GlobalExceptionMiddleware>();
app.UseStaticFiles(); // <--- Asta face folderul wwwroot public

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Comanda magică: Aplică toate migrările care lipsesc (echivalentul 'dotnet ef database update')
        // Dacă baza nu există, o creează. Dacă există, o actualizează.
        context.Database.Migrate(); 
        
        Console.WriteLine("Migrarea bazei de date a reușit!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"A apărut o eroare la migrare: {ex.Message}");
    }
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "api c# by silviu");

app.MapGet("/public-data", () => "Endpoint public OK");
app.MapGet("/secure-data", () => "Ai acces la endpoint securizat")
    .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

app.Run();
