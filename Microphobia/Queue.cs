using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.Utilities.Extensions;

namespace N17Solutions.Microphobia
{
    public class Queue
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaHubContext _taskHubContext;
        
        public Queue(IDataProvider dataProvider, MicrophobiaHubContext taskHubContext)
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
                await _dataProvider.Save(task).ConfigureAwait(false);

                await _taskHubContext.RefreshTasks().ConfigureAwait(false);
            }
        }
        
        private async Task Enqueue(TaskInfo taskInfo)
        {
            taskInfo.Status = TaskStatus.Created;
            
            await _dataProvider.Enqueue(taskInfo).ConfigureAwait(false);
            await _taskHubContext.RefreshTasks().ConfigureAwait(false);
        }
    }
}