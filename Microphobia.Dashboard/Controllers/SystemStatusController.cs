using System;
using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract.Configuration;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    public class SystemStatusRequest
    {
        public string Operation { get; set; }    
    }
    
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

        [HttpPatch]
        public IActionResult Patch([FromBody]SystemStatusRequest request)
        {
            if (request.Operation.Equals("start", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!_client.IsRunning)
                    _client.Start();
            }
            else
            {
                _client.Stop();
            }

            return Ok();
        }
    }
}