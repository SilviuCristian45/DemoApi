using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

var keycloakConfig = builder.Configuration.GetSection("Keycloak");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // <--- SCHIMBARE: Citim din config, nu hardcodat
        options.Authority = keycloakConfig["Authority"] ?? "http://localhost:8080/realms/myrealm"; 
        options.Audience = keycloakConfig["ClientId"] ?? "myclient";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            RoleClaimType = "realm_access.roles",
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ---> ADAUGĂ ACESTE DOUĂ LINII <---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 
// ----------------------------------

var app = builder.Build();

// De obicei Swagger e activat doar în Development (ca să nu expui API-ul public)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Generează fișierul .json (ex: /swagger/v1/swagger.json)
    app.UseSwaggerUI(); // Generează interfața grafică HTML (ex: /swagger/index.html)
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (HttpContext http, IConfiguration config) =>
{
    using var httpClient = new HttpClient();

    var keycloakUrl = config["Keycloak:TokenUrl"];
    using var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        {"grant_type", "password"},
        {"client_id", config["Keycloak:ClientId"]!}, 
        {"client_secret", config["Keycloak:ClientSecret"]!},
        {"username", http.Request.Form["username"]!},
        {"password", http.Request.Form["password"]!}
    });

    var response = await httpClient.PostAsync(keycloakUrl, form);
    var content = await response.Content.ReadAsStringAsync();

    return Results.Content(content, "application/json");
});

app.MapGet("/public-data", () => "Endpoint public OK");

app.MapGet("/secure-data", () => "Ai acces la endpoint securizat")
    .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

app.Run();
