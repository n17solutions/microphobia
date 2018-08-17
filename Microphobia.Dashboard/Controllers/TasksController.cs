using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia.Dashboard.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;

namespace N17Solutions.Microphobia.Dashboard.Controllers
{
    [Route("api/[controller]")]
    public class TasksController : DashboardController
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaHubContext _hubContext;
        
        public TasksController(IDataProvider dataProvider, MicrophobiaHubContext hubContext)
        {
            _dataProvider = dataProvider;
            _hubContext = hubContext;
        }
        
        [HttpGet]
        public async Task<IActionResult> ListTasks()
        {
            var tasks = (await _dataProvider.GetTasks())
                .Select(task => new TaskInfoReadModel(task));

            return Ok(tasks);
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTask(Guid taskId)
        {
            var task = new TaskInfoReadModel(await _dataProvider.GetTaskInfo(taskId));
            return Ok(task);
        }

        [HttpPatch("{taskId}")]
        public async Task<IActionResult> ReenqueueTask(Guid taskId)
        {
            var task = await _dataProvider.GetTaskInfo(taskId);
            task.Status = TaskStatus.Created;
            await _dataProvider.Save(task);

            await _hubContext.RefreshTasks();

            return Ok();
        }
    }
}