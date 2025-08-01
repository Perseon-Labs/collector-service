﻿using CollectorService.Contracts;
using CollectorService.Models;
using Microsoft.Extensions.Caching.Hybrid;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace CollectorService.Services
{
    [McpServerToolType]
    public class ElectricityPriceService(IHttpClientFactory httpClientFactory, HybridCache cache) : IElectricityPriceService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly HybridCache _cache = cache;
        private static readonly Lock _lock = new Lock();

        [McpServerTool]
        [Description("Gets todays electricity prices for swedish regions SE1, SE2, SE3 and SE4")]
        public async Task<Result<object>> GetElectricityPricesAsync()
        {
            try
            {
                var res = await _cache.GetOrCreateAsync("ElectricityPrices", async cToken =>
                {
                    return await GetPrices();
                }, new HybridCacheEntryOptions() { Expiration = TimeSpan.FromHours(6) });

                return Result<object>.Success(res);
            }
            catch (Exception ex)
            {
                return Result<object>.Failure($"Failed to fetch electricity prices: {ex.Message}");
            }

        }

        private async Task<object> GetPrices()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var now = DateTime.Now;

            var se1 = await httpClient.GetAsync($"https://www.elprisetjustnu.se/api/v1/prices/{now.Year}/{now:MM}-{now:dd}_SE1.json");
            var se2 = await httpClient.GetAsync($"https://www.elprisetjustnu.se/api/v1/prices/{now.Year}/{now:MM}-{now:dd}_SE2.json");
            var se3 = await httpClient.GetAsync($"https://www.elprisetjustnu.se/api/v1/prices/{now.Year}/{now:MM}-{now:dd}_SE3.json");
            var se4 = await httpClient.GetAsync($"https://www.elprisetjustnu.se/api/v1/prices/{now.Year}/{now:MM}-{now:dd}_SE4.json");

            se1.EnsureSuccessStatusCode();
            se2.EnsureSuccessStatusCode();
            se3.EnsureSuccessStatusCode();
            se4.EnsureSuccessStatusCode();

            var se1Content = await se1.Content.ReadFromJsonAsync<ElectricityPrice[]>();
            var se2Content = await se2.Content.ReadFromJsonAsync<ElectricityPrice[]>();
            var se3Content = await se3.Content.ReadFromJsonAsync<ElectricityPrice[]>();
            var se4Content = await se4.Content.ReadFromJsonAsync<ElectricityPrice[]>();

            return new
            {
                se1 = se1Content,
                se2 = se2Content,
                se3 = se3Content,
                se4 = se4Content
            };
        }
    }
}
