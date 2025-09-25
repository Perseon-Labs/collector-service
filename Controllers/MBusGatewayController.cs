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
        public async Task<ActionResult<MeterValueResult>> ReadMeterValue(string deviceId)
        {
            var result = await _gatewayService.ReadMeterValueAsync(deviceId);
            if (!result.Success)
                return BadRequest(result.ErrorMessage);
            return Ok(result);
        }

        [HttpGet("read-all")]
        public async Task<ActionResult<IEnumerable<MeterValueResult>>> ReadAllMeterValues()
        {
            var results = await _gatewayService.ReadAllMeterValuesAsync();
            return Ok(results);
        }
    }
}
