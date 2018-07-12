using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace N17Solutions.Microphobia
{
    public class Queue
    {
        private readonly IDataProvider _dataProvider;
        private readonly IHubContext<MicrophobiaHub> _taskHubContext;

        public Queue(IDataProvider dataProvider, IHubContext<MicrophobiaHub> taskHubContext)
        {
            _dataProvider = dataProvider;
            _taskHubContext = taskHubContext;
        }

        public Task Enqueue(Expression<Action> expression)
        {
            var taskInfo = expression.ToTaskInfo();
            return Enqueue(taskInfo);
        }

        public Task Enqueue<TExecutor>(Expression<Action<TExecutor>> expression)
        {
            var taskInfo = expression.ToTaskInfo();
            return Enqueue(taskInfo);
        }
        
        public async Task<TaskInfo> Dequeue()
        {
            var dequeuedTask = await _dataProvider.Dequeue().ConfigureAwait(false);
            await SetQueuedTaskWaitingToRun(dequeuedTask).ConfigureAwait(false);

            return dequeuedTask;
        }

        public Task SetQueuedTaskRunning(TaskInfo task) => SetQueuedTaskStatus(task, TaskStatus.Running);

        public Task SetQueuedTaskCompleted(TaskInfo task) => SetQueuedTaskStatus(task, TaskStatus.RanToCompletion);

        public Task SetQueuedTaskWaitingToRun(TaskInfo task) => SetQueuedTaskStatus(task, TaskStatus.WaitingToRun);

        public Task SetQueuedTaskFaulted(TaskInfo task, Exception ex)
        {
            var detailsBuilder = new StringBuilder();
            var exception = ex;
            var exceptionDepth = 1;
            
            while (exception != null)
            {
                if (exceptionDepth > 1)
                    detailsBuilder.AppendLine("-------------------------------");    
                
                detailsBuilder.AppendLine($"Exception Level: {exceptionDepth++}");
                detailsBuilder.AppendLine($"Message: {exception.Message}");
                detailsBuilder.AppendLine($"Stack Trace: {exception.StackTrace}");
                detailsBuilder.AppendLine($"Source: {exception.Source}");
                
                exception = exception.InnerException;
            }

            task.FailureDetails = detailsBuilder.ToString();

            return SetQueuedTaskStatus(task, TaskStatus.Faulted);
        }

        private async Task SetQueuedTaskStatus(TaskInfo task, TaskStatus status)
        {
            if (task != null)
            {
                task.Status = status;
                await _dataProvider.Save(task);

                await MicrophobiaHubActions.RefreshTasks(_taskHubContext.Clients.All);
            }
        }
        
        private Task Enqueue(TaskInfo taskInfo)
        {
            taskInfo.Status = TaskStatus.Created;
            return Task.WhenAll(_dataProvider.Enqueue(taskInfo), MicrophobiaHubActions.RefreshTasks(_taskHubContext.Clients.All));
        }
    }
}