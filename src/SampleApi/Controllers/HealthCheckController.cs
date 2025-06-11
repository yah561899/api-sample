using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleApi.Models.Enums;
using SampleApi.Utils.Tools;

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

        [HttpGet("redis/info")]
        public IActionResult GetRedisInfo()
        {
            var redisHost = EnvironmentVariableReader<EnumEnvironmentVariable>.Get(EnumEnvironmentVariable.RedisHost);
            var redisPort = EnvironmentVariableReader<EnumEnvironmentVariable>.Get(EnumEnvironmentVariable.RedisPort);
            var res = new
            {
                redisHost, redisPort,
            };

            return Ok(res);
        }
    }
}
