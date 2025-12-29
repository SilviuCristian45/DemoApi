using DemoApi.Models;
using DemoApi.Utils;
using FS.Keycloak.RestApiClient.Api;
using FS.Keycloak.RestApiClient.Authentication.ClientFactory;
using FS.Keycloak.RestApiClient.Authentication.Flow;
using FS.Keycloak.RestApiClient.ClientFactory;
using FS.Keycloak.RestApiClient.Model;
using Microsoft.Extensions.Caching.Memory;

namespace DemoApi.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    private readonly ClientCredentialsFlow _authFlow;

    private readonly string _targetRealm;

    private readonly IMemoryCache _cache;

    private readonly MemoryCacheEntryOptions _cacheOptions;

    // Injectăm dependențele exact ca înainte, dar acum în Service
    public AuthService(IConfiguration configuration, IWebHostEnvironment env, IMemoryCache cache)
    {
        _configuration = configuration;

        // Logica de SSL Bypass (păstrată aici)
        var handler = new HttpClientHandler();
        if (env.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        _httpClient = new HttpClient(handler);

        _authFlow = new ClientCredentialsFlow
        {
            KeycloakUrl = _configuration["Keycloak:Url"] ?? "test",  // fără / la final
            Realm = _configuration["Keycloak:Realm"] ?? "no_realm",                                 // realm-ul în care e client-ul admin
            ClientId = _configuration["Keycloak:ClientId"] ?? "clientid",                    // client-ul creat mai sus
            ClientSecret = _configuration["Keycloak:ClientSecret"] ?? "secret-discret"
        };

        _targetRealm = _configuration["Keycloak:Realm"] ?? "no_realm";  // ex: "myapp"
        _cache = cache;

        _cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(300)) // Expiră în 5 min
                .SetSlidingExpiration(TimeSpan.FromMinutes(300)); // Sau dacă nu e accesat 2 min
    }

    public async Task<ServiceResult<string>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            return await CreateUserInKeycloakAsync(request);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return ServiceResult<string>.Fail(e.Message.ToString() ?? "ceva");
        }
    }

    public async Task<PaginatedResponse<UserResponse>> GetAllUsers(PaginatedQueryDto paginatedQueryDto) {
        using var httpClient = AuthenticationHttpClientFactory.Create(_authFlow);
        // Inițializăm API-ul pentru Users
        using var usersApi = ApiClientFactory.Create<UsersApi>(httpClient);
        using var usersRolesApi = ApiClientFactory.Create<RoleMapperApi>(httpClient);

        string cacheUsersKey = $"cache_users_${paginatedQueryDto.PageNumber}_${paginatedQueryDto.PageSize}_${paginatedQueryDto.Search}";
        string totalCacheUsersKey = $"total_${cacheUsersKey}";

        List<UserRepresentation> users = new List<UserRepresentation>();
        int total = 0;

        Boolean areUsersInCache = _cache.TryGetValue(cacheUsersKey, out users);
        Boolean isTotalUsersInCache = _cache.TryGetValue(totalCacheUsersKey, out total);

        if (!areUsersInCache && !isTotalUsersInCache) {
            users = await usersApi.GetUsersAsync(_targetRealm, first: paginatedQueryDto.PageNumber, max: paginatedQueryDto.PageSize, search: paginatedQueryDto.Search);
            total = await usersApi.GetUsersCountAsync(_targetRealm, search: paginatedQueryDto.Search);

            var tasks = users.Select(async user => 
            {
                var mappings = await usersRolesApi.GetUsersRoleMappingsByUserIdAsync(_targetRealm, user.Id);
                
                if (mappings?.RealmMappings != null)
                {
                    // Logica ta de filtrare roluri
                    user.RealmRoles = mappings.RealmMappings
                        .Select(r => r.Name)
                        .Where(r => Enum.TryParse(typeof(Role), r, out _)) // Filtrăm doar rolurile valide
                        .ToList();
                }
                else
                {
                    user.RealmRoles = new List<string>();
                }
            });

            await Task.WhenAll(tasks);
            
            _cache.Set(cacheUsersKey, users, _cacheOptions);
            _cache.Set(totalCacheUsersKey, total, _cacheOptions);

            Console.WriteLine("citim useri  din keycloak");
        } else {
            Console.WriteLine("citim useri din RAM");
        }
        
        return new PaginatedResponse<UserResponse>(
            total, 
            users.Select(
                user => new UserResponse {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    Roles = user.RealmRoles,
                }
            ).ToList() 
        );
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var tokenUrl = _configuration["Keycloak:TokenUrl"];
        Console.WriteLine($" token url is {tokenUrl}");
        var keycloakParams = new Dictionary<string, string>
        {
            {"grant_type", "password"},
            {"client_id", _configuration["Keycloak:ClientId"]!},
            {"client_secret", _configuration["Keycloak:ClientSecret"]!},
            {"username", request.Username},
            {"password", request.Password}
        };

        using var form = new FormUrlEncodedContent(keycloakParams);

        // Call extern
        var response = await _httpClient.PostAsync(tokenUrl, form);

        // 1. Gestionare Eroare
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return ApiResponse<LoginResponse>.Error($"Keycloak Error: {errorContent}");
        }

        // 2. Gestionare Succes
        var tokenData = await response.Content.ReadFromJsonAsync<LoginResponse>();

        if (tokenData == null)
        {
            return ApiResponse<LoginResponse>.Error("Răspuns invalid de la server.");
        }

        return ApiResponse<LoginResponse>.Success(tokenData);
    }

    public async Task<ServiceResult<string>> CreateUserInKeycloakAsync(RegisterRequest request)
    {
        // Configurația pentru autentificare admin (client credentials flow)
        

        // Creăm HttpClient-ul autentificat
        using var httpClient = AuthenticationHttpClientFactory.Create(_authFlow);

        // Inițializăm API-ul pentru Users
        using var usersApi = ApiClientFactory.Create<UsersApi>(httpClient);
        using var rolesApi = ApiClientFactory.Create<RolesApi>(httpClient);
        using var userRolesApi = ApiClientFactory.Create<RoleMapperApi>(httpClient);

        // Realm-ul în care vrei să creezi utilizatorul (poate fi diferit de master!)
        

        var allRoles = await rolesApi.GetRolesAsync(_targetRealm);
        var adminRole = allRoles.FirstOrDefault(r => r.Name == request.Role); // sau "admin"

        if (adminRole == null) {
            return ServiceResult<string>.Fail($"Rolul  {request.Role} nu este setat");
        }

        // Datele utilizatorului nou
        var newUser = new UserRepresentation
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = "Ion",
            LastName = "Popescu",
            Enabled = true,
            EmailVerified = false,  // poți pune true dacă vrei
            RealmRoles = new List<string>() { Role.ADMIN.ToString() }
        };

        // Parolă (opțional: dacă vrei să setezi una fixă)
        newUser.Credentials = new List<CredentialRepresentation>
        {
            new CredentialRepresentation
            {
                Type = "password",
                Value = request.Password,      // parola inițială
                Temporary = false          // false = permanentă, true = utilizatorul trebuie să o schimbe la prima logare
            }
        };

        var createResponse = await usersApi.PostUsersWithHttpInfoAsync(_targetRealm, newUser);

        if (createResponse.StatusCode != System.Net.HttpStatusCode.Created)
        {
            var errorBody = createResponse.Content.ToString();
            throw new Exception($"Eroare Keycloak ({createResponse.StatusCode}): {errorBody}");
        }

        // 2. Extrage user ID din Location header
        // Extrage Location header-ul
        if (!createResponse.Headers.TryGetValue("Location", out var locationValues) ||
            locationValues == null || !locationValues.Any())
        {
            throw new Exception("Location header lipsă în răspunsul Keycloak");
        }

        string locationUrl = locationValues.First();  // prima (și singura) valoare
        string userId = locationUrl.Split('/').Last();

        Console.WriteLine($"User ID extras: {userId}");

        if (adminRole != null)
        {
            var rolesToAdd =
                new List<RoleRepresentation>
                {
                    new RoleRepresentation
                    {
                        Id = adminRole.Id,
                        Name = adminRole.Name,
                        Composite = adminRole.Composite,
                        ClientRole = adminRole.ClientRole,
                        ContainerId = adminRole.ContainerId
                    }
                };

            await userRolesApi.PostUsersRoleMappingsRealmByUserIdAsync(_targetRealm, userId, rolesToAdd);

            Console.WriteLine($"Rol '{adminRole.Name}' asignat cu succes utilizatorului {userId}");

            return ServiceResult<string>.Ok("Utilizator creat cu success");
        }
        return ServiceResult<string>.Fail("Esec la creare utilizator");
    }

    private async Task<string> GetAdminToken()
    {
        var username = _configuration["Keycloak:Admin"] ?? "";
        var password = _configuration["Keycloak:AdminPassword"] ?? "";
        var client = new HttpClient();
        var tokenRequest = await LoginAsync(new LoginRequest(username, password));
        var token = tokenRequest.Data;
        return token?.AccessToken ?? "";
    }

}