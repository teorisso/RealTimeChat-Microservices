using Microsoft.Extensions.Caching.Memory;
using Shared.DTOs;
using Shared.Responses;
using System.Net.Http.Headers;

namespace MessagesService.Services
{
    public class UserInfoService : IUserInfoService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserInfoService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CACHE_KEY = "user_names_cache";

        public UserInfoService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<UserInfoService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> GetUserNameAsync(int userId)
        {
            var userNames = await GetAllUserNamesAsync();
            return userNames.TryGetValue(userId, out var name) ? name : $"Usuario {userId}";
        }

        public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds)
        {
            var allNames = await GetAllUserNamesAsync();
            var result = new Dictionary<int, string>();

            foreach (var userId in userIds)
            {
                result[userId] = allNames.TryGetValue(userId, out var name) ? name : $"Usuario {userId}";
            }

            return result;
        }

        private async Task<Dictionary<int, string>> GetAllUserNamesAsync()
        {
            // Intentar obtener del caché
            if (_cache.TryGetValue(CACHE_KEY, out Dictionary<int, string>? cachedNames) && cachedNames != null)
            {
                return cachedNames;
            }

            try
            {
                // CRÍTICO: Propagar JWT del usuario actual (REQ-10 compliance)
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));
                }

                // Llamar a AuthService con JWT propagado
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<UsuarioDto>>>("/api/auth/users");

                if (response?.Success == true && response.Data != null)
                {
                    var userNames = new Dictionary<int, string>();

                    foreach (var usuario in response.Data)
                    {
                        if (int.TryParse(usuario.Id, out var userId))
                        {
                            userNames[userId] = usuario.Nombre;
                        }
                    }

                    // Cachear con expiración de 5 minutos
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    };
                    _cache.Set(CACHE_KEY, userNames, cacheOptions);

                    return userNames;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Error al consultar AuthService para nombres de usuarios");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al obtener nombres de usuarios");
            }

            // Fallback: diccionario vacío
            return new Dictionary<int, string>();
        }
    }
}
