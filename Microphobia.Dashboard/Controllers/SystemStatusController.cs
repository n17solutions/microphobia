using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.Configuration;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    [Route("api/[controller]")]
    public class SystemStatusController : DashboardController
    {
        private readonly MicrophobiaConfiguration _config;
        private readonly Client _client;

        public SystemStatusController(MicrophobiaConfiguration config, Client client)
        {
            _config = config;
            _client = client;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_config.IsRunning);
        }

        [HttpPut]
        [Route("stop")]
        public IActionResult StopClient()
        {
            _client.Stop();
            return Ok();
        }

        [HttpPut]
        [Route("start")]
        public IActionResult StartClient()
        {
            _client.Start();
            return Ok();
        }
    }
}