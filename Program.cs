using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DemoApi.Utils;
using System.Security.Claims; // <--- OBLIGATORIU
using System.Text.Json;       // <--- OBLIGATORIU

using DemoApi.Hubs;
using DemoApi.Services;
using DemoApi.Data;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


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
