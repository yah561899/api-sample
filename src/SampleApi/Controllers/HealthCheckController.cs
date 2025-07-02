using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SampleApi.CommonUtils.Models.Enums;
using SampleApi.CommonUtils.Tools.Env;

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
            var redisHost = EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisHost);
            var redisPort = EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisPort);
            var res = new
            {
                redisHost, redisPort,
            };

            return Ok(res);
        }
    }
}
