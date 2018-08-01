using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace Microphobia.Dashboard.Harness.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class WebsocketController : ControllerBase
    {
        private readonly MicrophobiaHubContext _hubContext;

        public WebsocketController(MicrophobiaHubContext hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            await _hubContext.RefreshTasks();
            return Ok();
        }
    }
}