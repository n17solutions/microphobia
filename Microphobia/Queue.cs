using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
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

        public Task Enqueue(Expression<Action> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public Task Enqueue<TExecutor>(Expression<Action<TExecutor>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public Task Enqueue(Expression<Func<Task>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }
        
        public Task Enqueue<TExecutor>(Expression<Func<TExecutor, Task>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }
        
        public async Task<TaskInfo> DequeueSingle(string tag = default, CancellationToken cancellationToken = default)
        {
            var dequeuedTask = await _dataProvider.DequeueSingle(tag, cancellationToken).ConfigureAwait(false);
            await SetQueuedTaskWaitingToRun(dequeuedTask).ConfigureAwait(false);

            return dequeuedTask;
        }
        
        public async Task<IEnumerable<TaskInfo>> Dequeue(string tag = default, int? limit = null, CancellationToken cancellationToken = default)
        {
            var dequeuedTasks = await _dataProvider.Dequeue(tag, limit, cancellationToken).ConfigureAwait(false);
            var taskInfos = dequeuedTasks as TaskInfo[] ?? dequeuedTasks.ToArray();
            await Task.WhenAll(taskInfos.Select(SetQueuedTaskWaitingToRun)).ConfigureAwait(false);

            return taskInfos;
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