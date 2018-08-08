using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceResolution;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Websockets.Hubs;

namespace N17Solutions.Microphobia
{
    public class Queue
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaHubContext _taskHubContext;
        private readonly MicrophobiaConfiguration _config;

        public Queue(IDataProvider dataProvider, MicrophobiaHubContext taskHubContext, MicrophobiaConfiguration config)
        {
            _dataProvider = dataProvider;
            _taskHubContext = taskHubContext;
            _config = config;
        }

        public Task Enqueue(Expression<Action> expression, ServiceFactory scopedServiceFactory = null)
        {
            var taskInfo = expression.ToTaskInfo();
            return Enqueue(taskInfo, scopedServiceFactory);
        }

        public Task Enqueue<TExecutor>(Expression<Action<TExecutor>> expression, ServiceFactory scopedServiceFactory = null)
        {
            var taskInfo = expression.ToTaskInfo();
            return Enqueue(taskInfo, scopedServiceFactory);
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

                await _taskHubContext.RefreshTasks();
            }
        }
        
        private Task Enqueue(TaskInfo taskInfo, ServiceFactory scopedServiceFactory = null)
        {
            if (scopedServiceFactory != null)
                _config.ScopedServiceFactories.AddOrUpdate(taskInfo.Id, scopedServiceFactory, (guid, factory) => scopedServiceFactory);
            
            taskInfo.Status = TaskStatus.Created;
            return Task.WhenAll(_dataProvider.Enqueue(taskInfo), _taskHubContext.RefreshTasks());
        }
    }
}