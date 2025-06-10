using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SampleApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        private readonly ILogger<HealthCheckController> _logger;

        public HealthCheckController(ILogger<HealthCheckController> logger)
        {
            _logger = logger;
        }

        [HttpGet("readiness")]
        public IActionResult HealthCheck()
        {
            _logger.LogInformation("heathy status");
            return Ok("Ok");
        }

        [HttpGet("readinessV2")]
        public IActionResult HealthCheckV2()
        {
            _logger.LogInformation("heathy status v2");
            return Ok("Ok");
        }

        [HttpGet("readinessV3")]
        public IActionResult HealthCheckV3()
        {
            _logger.LogInformation("heathy status v3");
            return Ok("Ok");
        }
    }
}
