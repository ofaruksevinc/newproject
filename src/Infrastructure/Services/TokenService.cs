using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OrderManagementAPI.Infrastructure.Services
{
    public class TokenResponse
    {
        public string token_type { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string access_token { get; set; } = string.Empty;
    }

    public interface ITokenService
    {
        Task<string> GetTokenAsync();
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _tokenEndpoint;
        private readonly string _clientId;
        private readonly string _clientSecret;
        
        private const string TOKEN_CACHE_KEY = "AUTH_TOKEN";
        private const string HOURLY_REQUEST_COUNT_KEY = "TOKEN_REQUEST_COUNT";
        private const string HOURLY_RESET_TIME_KEY = "TOKEN_RESET_TIME";
        private const int MAX_HOURLY_REQUESTS = 5;
        
        public TokenService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<TokenService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
            
            // Gerçek uygulamada bu değerler appsettings.json'dan alınabilir
            _tokenEndpoint = _configuration["AuthService:TokenEndpoint"] ?? "https://api.example.com/auth/token";
            _clientId = _configuration["AuthService:ClientId"] ?? "client_id";
            _clientSecret = _configuration["AuthService:ClientSecret"] ?? "client_secret";
        }
        
        public async Task<string> GetTokenAsync()
        {
            // Önce cache'den token'ı kontrol et
            if (_cache.TryGetValue(TOKEN_CACHE_KEY, out string cachedToken))
            {
                _logger.LogInformation("Önbellekten token alındı");
                return cachedToken;
            }
            
            // Token yoksa veya süresi dolmuşsa, yeni token almak için semaforu kullan
            // Bu, aynı anda birden fazla token isteği yapılmasını engeller
            await _semaphore.WaitAsync();
            try
            {
                // Semaforu aldıktan sonra tekrar cache'i kontrol et (double-check locking)
                if (_cache.TryGetValue(TOKEN_CACHE_KEY, out string tokenAfterLock))
                {
                    _logger.LogInformation("Semafor sonrası önbellekten token alındı");
                    return tokenAfterLock;
                }
                
                // Saatlik istek sınırını kontrol et
                if (!CanMakeTokenRequest())
                {
                    // Sınıra ulaşıldıysa ve önbellekte token yoksa, bir hata fırlat
                    throw new InvalidOperationException("Saatlik token istek sınırına ulaşıldı. Lütfen daha sonra tekrar deneyin.");
                }
                
                // Yeni token al
                _logger.LogInformation("Yeni token talep ediliyor");
                var response = await RequestNewTokenAsync();
                
                // Saatlik istek sayısını artır
                IncrementRequestCounter();
                
                // Token'ı önbelleğe al (süresinden biraz önce expire et, güvenlik marjı için)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(response.expires_in * 0.9));
                
                _cache.Set(TOKEN_CACHE_KEY, response.access_token, cacheOptions);
                
                _logger.LogInformation("Yeni token alındı ve önbelleğe kaydedildi. Geçerlilik süresi: {ExpiresIn} saniye", response.expires_in);
                
                return response.access_token;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private async Task<TokenResponse> RequestNewTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret)
            });
            
            var response = await client.PostAsync(_tokenEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token alınamadı. Durum kodu: {StatusCode}, Hata: {Error}", (int)response.StatusCode, errorContent);
                throw new HttpRequestException($"Token alınamadı. Durum kodu: {(int)response.StatusCode}");
            }
            
            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null)
            {
                throw new InvalidOperationException("Token yanıtı boş");
            }
            
            return tokenResponse;
        }
        
        private bool CanMakeTokenRequest()
        {
            // Saatlik istek sınırlaması mantığı
            
            // Reset zamanını kontrol et
            if (!_cache.TryGetValue(HOURLY_RESET_TIME_KEY, out DateTime resetTime))
            {
                // İlk istek, reset zamanını bir saat sonrası olarak ayarla
                resetTime = DateTime.UtcNow.AddHours(1);
                _cache.Set(HOURLY_RESET_TIME_KEY, resetTime, resetTime - DateTime.UtcNow);
            }
            
            // Eğer reset zamanı geçmişse, sayacı sıfırla
            if (DateTime.UtcNow >= resetTime)
            {
                _cache.Set(HOURLY_REQUEST_COUNT_KEY, 0, TimeSpan.FromHours(1));
                resetTime = DateTime.UtcNow.AddHours(1);
                _cache.Set(HOURLY_RESET_TIME_KEY, resetTime, TimeSpan.FromHours(1));
                _logger.LogInformation("Saatlik token istek sayacı sıfırlandı");
            }
            
            // Mevcut istek sayısını al
            _cache.TryGetValue(HOURLY_REQUEST_COUNT_KEY, out int requestCount);
            
            // İstek sayısı sınırın altındaysa, istek yapılabilir
            return requestCount < MAX_HOURLY_REQUESTS;
        }
        
        private void IncrementRequestCounter()
        {
            // Mevcut istek sayısını al ve artır
            _cache.TryGetValue(HOURLY_REQUEST_COUNT_KEY, out int requestCount);
            requestCount++;
            
            // Sayacı güncelle
            _cache.Set(HOURLY_REQUEST_COUNT_KEY, requestCount, TimeSpan.FromHours(1));
            _logger.LogInformation("Saatlik token istek sayacı güncellendi: {RequestCount}/{MaxRequests}", requestCount, MAX_HOURLY_REQUESTS);
        }
    }
} 