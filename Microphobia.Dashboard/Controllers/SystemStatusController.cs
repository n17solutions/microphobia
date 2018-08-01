using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.Configuration;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    [Route("api/[controller]")]
    public class SystemStatusController : DashboardController
    {
        private readonly MicrophobiaConfiguration _config;

        public SystemStatusController(MicrophobiaConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_config.IsRunning);
        }
    }
}