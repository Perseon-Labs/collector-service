using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollectorService.Contracts
{
    public interface IMBusGatewayService
    {
        Task<MeterValueResult> ReadMeterValueAsync(string deviceId);
        Task<IEnumerable<MeterValueResult>> ReadAllMeterValuesAsync();
    }

    public class MeterValueResult
    {
        public string DeviceId { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
