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
        private readonly IMBusGatewayService _gatewayService;
        private readonly ILogger<MBusGatewayBackgroundService> _logger;

        public MBusGatewayBackgroundService(IMBusGatewayService gatewayService, ILogger<MBusGatewayBackgroundService> logger)
        {
            _gatewayService = gatewayService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var results = await _gatewayService.ReadAllMeterValuesAsync();
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
