using CollectorService.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollectorService.Services
{
    public class MBusGatewayService : IMBusGatewayService
    {
        public async Task<MeterValueResult> ReadMeterValueAsync(string deviceId)
        {
            // Simulate reading from device
            await Task.Delay(100); // Simulate I/O
            return new MeterValueResult
            {
                DeviceId = deviceId,
                Value = 123.45, // Simulated value
                Unit = "kWh",
                Success = true,
                ErrorMessage = null
            };
        }

        public async Task<IEnumerable<MeterValueResult>> ReadAllMeterValuesAsync()
        {
            // Simulate reading from multiple devices
            await Task.Delay(200); // Simulate I/O
            var results = new List<MeterValueResult>
            {
                new MeterValueResult { DeviceId = "device1", Value = 123.45, Unit = "kWh", Success = true },
                new MeterValueResult { DeviceId = "device2", Value = 67.89, Unit = "kWh", Success = true }
            };
            return results;
        }
    }
}
