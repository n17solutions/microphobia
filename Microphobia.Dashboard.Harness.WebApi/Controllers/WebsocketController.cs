using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace Microphobia.Dashboard.Harness.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class WebsocketController : ControllerBase
    {
        private readonly IHubContext<MicrophobiaHub> _hubContext;

        public WebsocketController(IHubContext<MicrophobiaHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            await MicrophobiaHubActions.RefreshTasks(_hubContext.Clients.All);
            return Ok();
        }
    }
}