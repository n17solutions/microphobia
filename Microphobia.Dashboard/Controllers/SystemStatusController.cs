using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using N17Solutions.Microphobia.ServiceContract.Configuration;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    [Route("api/[controller]")]
    public class SystemStatusController : DashboardController
    {
        private readonly MicrophobiaConfiguration _config;
        private readonly Client _client;

        public SystemStatusController(MicrophobiaConfiguration config, IHostedService client)
        {
            _config = config;
            _client = (Client)client;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_config.IsRunning);
        }

        [HttpPut]
        [Route("stop")]
        public async Task<IActionResult> StopClient(CancellationToken cancellationToken)
        {
            await _client.StopAsync(cancellationToken);
            return Ok();
        }

        [HttpPut]
        [Route("start")]
        public async Task<IActionResult> StartClient(CancellationToken cancellationToken)
        {
            await _client.StartAsync(cancellationToken);
            return Ok();
        }
    }
}