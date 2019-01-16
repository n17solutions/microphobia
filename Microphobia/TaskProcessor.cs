using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.ServiceContract;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.ServiceContract.Websockets.Hubs;
using N17Solutions.Microphobia.ServiceResolution;

namespace N17Solutions.Microphobia
{
    public class TaskProcessor
    {
        private readonly IDataProvider _dataProvider;
        private readonly MicrophobiaHubContext _taskHubContext;
        private readonly ILogger<TaskProcessor> _logger;
        private readonly Runners _runners;
        private readonly MicrophobiaConfiguration _config;

        public TaskProcessor(IDataProvider dataProvider, MicrophobiaHubContext taskHubContext, ILogger<TaskProcessor> logger, Runners runners, MicrophobiaConfiguration config)
        {
            _dataProvider = dataProvider;
            _taskHubContext = taskHubContext;
            _logger = logger;
            _runners = runners;
            _config = config;
        }

        public async Task ProcessTask(TaskInfo task, ServiceFactory serviceFactory = null, CancellationToken cancellationToken = default)
        {
            await LogTaskStarted(task);

            try
            {
                var stopWatch = new Stopwatch();

                stopWatch.Start();
                ExecuteTask(task, serviceFactory ?? _config.ServiceFactory ?? Activator.CreateInstance, cancellationToken);
                stopWatch.Stop();

                await LogTaskCompleted(task, stopWatch.Elapsed);
            }
            catch (Exception ex)
            {
                await LogTaskException(task, ex);
            }

            await _runners.MarkTaskProcessedTime(_config.RunnerName, cancellationToken);
        }
        
        public async Task SetTaskStatus(TaskInfo task, TaskStatus status)
        {
            if (task != null)
            {
                task.Status = status;
                await _dataProvider.Save(task);
                await _taskHubContext.RefreshTasks();
            }
        }

        public Task SetQueuedTaskRunning(TaskInfo task) => SetTaskStatus(task, TaskStatus.Running);
        public Task SetQueuedTaskCompleted(TaskInfo task) => SetTaskStatus(task, TaskStatus.RanToCompletion);
        public Task SetQueuedTaskWaitingForRun(TaskInfo task) => SetTaskStatus(task, TaskStatus.WaitingToRun);

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

            return SetTaskStatus(task, TaskStatus.Faulted);
        }
        
        private Task LogTaskStarted(TaskInfo taskInfo)
        {
            var logMessage = Messages.TaskStarted(taskInfo);
            _logger.LogInformation(logMessage);

            return SetQueuedTaskRunning(taskInfo);
        }
        
        private async Task LogTaskCompleted(TaskInfo taskInfo, TimeSpan completionTime)
        {
            var logMessage = Messages.TaskFinished(taskInfo, completionTime);
            _logger.LogInformation(logMessage);

            await SetQueuedTaskCompleted(taskInfo);
        }
        
        private async Task LogTaskException(TaskInfo taskInfo, Exception ex)
        {
            var logMessage = Messages.TaskThrewException(taskInfo);
            _logger.LogError(ex, logMessage);

            await SetQueuedTaskFaulted(taskInfo, ex);
        }

        private static void ExecuteTask(TaskInfo taskInfo, ServiceFactory serviceFactory, CancellationToken cancellationToken)
        {
            var assembly = Assembly.Load(taskInfo.AssemblyName);
            var type = assembly.GetType(taskInfo.TypeName);

            var methodName = taskInfo.MethodName;
            var args = taskInfo.Arguments;

            object instance = null;
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                try
                {
                    instance = serviceFactory(type);

                    if (instance == null)
                        throw new Exception("Couldn't resolve type");
                }
                catch (Exception)
                {
                    throw new InvalidOperationException($"Cannot execute method {methodName} on type {type} as it cannot be resolved. Have you made sure to add it to your Dependency Injection?");
                }

                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            InvokeMethod(method, instance, args, cancellationToken);
        }

        private static void InvokeMethod(MethodBase method, object instance, object[] arguments, CancellationToken cancellationToken)
        {
            var result = method.Invoke(instance, arguments);

            if (!(result is Task task))
                return;

            task.Wait(cancellationToken);
        }
    }
}