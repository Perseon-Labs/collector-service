using CollectorService.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollectorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MBusGatewayController : ControllerBase
    {
        private readonly IMBusGatewayService _gatewayService;

        public MBusGatewayController(IMBusGatewayService gatewayService)
        {
            _gatewayService = gatewayService;
        }

        [HttpGet("read/{deviceId}")]
        public async Task<ActionResult<MeterReadingResult>> ReadMeter(string deviceId, CancellationToken ct)
        {
            var result = await _gatewayService.ReadMeterAsync(deviceId, ct);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("read-all")]
        public async Task<ActionResult<IEnumerable<MeterReadingResult>>> ReadAllMeters(CancellationToken ct)
        {
            var results = await _gatewayService.ReadAllMetersAsync(ct);
            return Ok(results);
        }
    }
}
