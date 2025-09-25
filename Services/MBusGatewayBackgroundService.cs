using CollectorService.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CollectorService.Services
{
    public class MBusGatewayBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MBusGatewayBackgroundService> _logger;

        public MBusGatewayBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MBusGatewayBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var gateway = scope.ServiceProvider.GetRequiredService<IMBusGatewayService>();
                    var results = await gateway.ReadAllMeterValuesAsync();
                    foreach (var result in results)
                    {
                        if (result.Success)
                            _logger.LogInformation($"Device {result.DeviceId}: {result.Value} {result.Unit}");
                        else
                            _logger.LogWarning($"Failed to read {result.DeviceId}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading meter values");
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Run every 5 minutes
            }
        }
    }
}
