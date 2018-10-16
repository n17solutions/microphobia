using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    [Route("api/[controller]")]
    public class RunnersController : DashboardController
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaHubContext _hubContext;
        
        public RunnersController(IDataProvider dataProvider, MicrophobiaHubContext hubContext)
        {
            _dataProvider = dataProvider;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> ListRunners()
        {
            var runners = await _dataProvider.GetRunners();
            return Ok(runners);
        }
    }
}