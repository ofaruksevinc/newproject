using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagementAPI.Core.DTOs;
using OrderManagementAPI.Core.Interfaces;

namespace OrderManagementAPI.Infrastructure.Services
{
    public class OrderSyncService : BackgroundService
    {
        private readonly ILogger<OrderSyncService> _logger;
        private readonly IServiceProvider _services;
        private readonly HttpClient _httpClient;
        private readonly string _ordersApiEndpoint;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5); // 5 dakikalık senkronizasyon aralığı
        
        public OrderSyncService(
            ILogger<OrderSyncService> logger,
            IServiceProvider services,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _services = services;
            _httpClient = httpClientFactory.CreateClient("OrdersAPI");
            _ordersApiEndpoint = configuration["ExternalServices:OrdersApiEndpoint"] ?? "https://api.example.com/orders";
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sipariş senkronizasyon servisi başlatıldı. Senkronizasyon aralığı: {Interval} dakika", _syncInterval.TotalMinutes);
            
            // İlk çalıştırma anında bir senkronizasyon yap
            await SynchronizeOrdersAsync();
            
            // Düzenli aralıklarla senkronizasyon yap
            using PeriodicTimer timer = new PeriodicTimer(_syncInterval);
            
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await SynchronizeOrdersAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sipariş senkronizasyon servisi durduruldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş senkronizasyonu sırasında beklenmeyen bir hata oluştu");
            }
        }
        
        private async Task SynchronizeOrdersAsync()
        {
            try
            {
                _logger.LogInformation("Sipariş senkronizasyonu başlatılıyor");
                
                // Servis scope oluştur (DI kullanmak için)
                using var scope = _services.CreateScope();
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                
                try
                {
                    // Token al
                    var token = await tokenService.GetTokenAsync();
                    
                    // HTTP isteği yap
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var response = await _httpClient.GetAsync(_ordersApiEndpoint);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Sipariş listesi alınamadı. Durum kodu: {StatusCode}, Hata: {Error}", 
                            (int)response.StatusCode, errorContent);
                        return;
                    }
                    
                    var orders = await response.Content.ReadFromJsonAsync<List<OrderResponseDto>>();
                    
                    if (orders == null || orders.Count == 0)
                    {
                        _logger.LogInformation("Senkronize edilecek sipariş bulunamadı");
                        return;
                    }
                    
                    _logger.LogInformation("{OrderCount} adet sipariş başarıyla senkronize edildi", orders.Count);
                    
                    // TODO: Burada siparişleri veritabanına işleme veya diğer işlemleri yapabilirsiniz
                    // Örneğin:
                    // await _orderRepository.SynchronizeOrdersAsync(orders);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("token istek sınırına ulaşıldı"))
                {
                    // Token istek limiti aşıldığında
                    _logger.LogWarning("Token istek limiti nedeniyle senkronizasyon atlanıyor: {Message}", ex.Message);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Sipariş listesi API isteği sırasında hata oluştu");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş senkronizasyonu sırasında beklenmeyen bir hata oluştu");
            }
        }
    }
} 