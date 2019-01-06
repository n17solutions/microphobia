using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.ServiceContract;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;
using Polly;
// ReSharper disable SuggestBaseTypeForParameter

namespace N17Solutions.Microphobia
{
    public class Client : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly MicrophobiaConfiguration _config;
        private readonly ILogger _logger;

        private CancellationToken _cancellationToken;
        private uint _nothingToDequeueCount;
        private bool _cancelled;

        public Client(IServiceScopeFactory serviceScopeFactory, MicrophobiaConfiguration config, ILogger<Client> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _config = config;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            
            Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetryAsync(new[] {TimeSpan.FromMilliseconds(_config.PollIntervalMs * 100)}, async (exception, timeSpan) =>
                {
                    await SetFailure(exception).ConfigureAwait(false);
                    await StopAsync(_cancellationToken).ConfigureAwait(false);
                    await StartAsync(_cancellationToken).ConfigureAwait(false);
                })
                .ExecuteAsync(() =>
                {
                    // If already cancelled, just bin out
                    _cancellationToken.ThrowIfCancellationRequested();

                    var task = Task.Run(async () =>
                    {
                        await SetStarted();
                        
                        while (true)
                        {
                            if (_cancellationToken.IsCancellationRequested || _cancelled)
                            {
                                await StopAsync(_cancellationToken).ConfigureAwait(false);
                                _cancellationToken.ThrowIfCancellationRequested();
                                
                                break;
                            }
                           
                            var tasksToProcess = new List<TaskInfo>();

                            using (var serviceScope = _serviceScopeFactory.CreateScope())
                            {
                                var queue = serviceScope.ServiceProvider.GetRequiredService<Queue>();

                                if (_config.MaxThreads == 1)
                                    tasksToProcess.Add(await queue.DequeueSingle(_config.Tag, cancellationToken).ConfigureAwait(false));
                                else
                                    tasksToProcess.AddRange(await queue.Dequeue(_config.Tag, cancellationToken: cancellationToken).ConfigureAwait(false));

                                tasksToProcess.RemoveAll(t => t == null);

                                _logger.LogInformation($"Tasks To Process: {tasksToProcess.Count}");

                                if (tasksToProcess.IsNullOrEmpty())
                                {
                                    if (_nothingToDequeueCount++ < _config.StopLoggingNothingToQueueAfter)
                                        _logger.LogInformation("Nothing to Dequeue");

                                    OnAllTasksProcessed(new EventArgs());
                                }
                                else
                                {
                                    _nothingToDequeueCount = 0;

                                    var threadChunks = tasksToProcess.Chunk(_config.MaxThreads);
                                    foreach (var chunk in threadChunks)
                                        await Task.WhenAll(chunk.Select(taskToProcess => ProcessTask(queue, taskToProcess))).ConfigureAwait(false);
                                }
                            }

                            await Task.Delay(_config.PollIntervalMs, cancellationToken).ConfigureAwait(false);
                        }
                    }, cancellationToken);
                    
                    task.ContinueWith(t => SetFailure(t.Exception?.InnerException), _cancellationToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default)
                        .ContinueWith(async t => await StopAsync(_cancellationToken), _cancellationToken, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);

                    return task;
                });

            return Task.CompletedTask;
        }

        private async Task ProcessTask(Queue queue, TaskInfo task)
        {
            _logger.LogInformation($"Dequeued Task: {task.Id}");
            await LogTaskStarted(task, queue).ConfigureAwait(false);
                
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                task.Execute(_config.ServiceFactory, _cancellationToken);

                stopWatch.Stop();
                await LogTaskCompleted(task, stopWatch.Elapsed, queue).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogTaskException(task, ex, queue).ConfigureAwait(false);
            }

            using (var serviceScope = _serviceScopeFactory.CreateScope())
            {
                var runners = serviceScope.ServiceProvider.GetRequiredService<Runners>();
                    await runners.MarkTaskProcessedTime(_config.RunnerName, _cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return SetStopped();
        }

        public void Dispose()
        {
            _cancelled = true;
        }
        
        public event EventHandler AllTasksProcessed;

        protected virtual void OnAllTasksProcessed(EventArgs e)
        {
            var handler = AllTasksProcessed;
            handler?.Invoke(this, e);
        }

        private async Task SetStarted()
        {
            var runnerName = _config.RunnerName;

            _cancelled = false;

            using (var serviceScope = _serviceScopeFactory.CreateScope())
            {
                var runners = serviceScope.ServiceProvider.GetRequiredService<Runners>();
                await runners.Clean(_cancellationToken).ConfigureAwait(false);
                var uniqueIndexer = await runners.Register(new QueueRunner
                {
                    Name = runnerName,
                    IsRunning = true
                }, _cancellationToken).ConfigureAwait(false);
                _config.SetRunnerIndexer(uniqueIndexer);
            }
            
            _logger.LogInformation($"Microphobia Client '{runnerName}' is starting...");
        }

        private async Task SetStopped()
        {
            var runnerName = _config.RunnerName;
            var runnerTag = _config.Tag;
            var runnerUniqueIndexer = _config.RunnerIndexer;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var runners = scope.ServiceProvider.GetRequiredService<Runners>();
                await runners.Deregister(runnerName, runnerTag, runnerUniqueIndexer, _cancellationToken).ConfigureAwait(false);
            }
            
            _logger.LogInformation($"Microphobia Client '{runnerName}' is stopping.");
        }
        
        private async Task SetFailure(Exception exception)
        {
            Guid logEntryId;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var systemLogProvider = scope.ServiceProvider.GetRequiredService<ISystemLogProvider>();
                logEntryId = await systemLogProvider.Log(new SystemLog
                {
                    Message = exception.Message,
                    Source = exception.Source,
                    StackTrace = exception.StackTrace,
                    Level = LogLevel.Error,
                    Data = exception
                }, _config.StorageType).ConfigureAwait(false);
            }
    
            _logger.LogCritical($"An error occurred while running the Microphobia Client and has been logged to the System Log with Resource Id: {logEntryId}");
        }
        
        private async Task LogTaskStarted(TaskInfo taskInfo, Queue queue)
        {
            var logMessage = Messages.TaskStarted(taskInfo);
            _logger.LogInformation(logMessage);

            await queue.SetQueuedTaskRunning(taskInfo);
        }
        
        private async Task LogTaskCompleted(TaskInfo taskInfo, TimeSpan completionTime, Queue queue)
        {
            var logMessage = Messages.TaskFinished(taskInfo, completionTime);
            _logger.LogInformation(logMessage);
            
            await queue.SetQueuedTaskCompleted(taskInfo);
        }
        
        private async Task LogTaskException(TaskInfo taskInfo, Exception ex, Queue queue)
        {
            var logMessage = Messages.TaskThrewException(taskInfo);
            _logger.LogError(ex, logMessage);

            await queue.SetQueuedTaskFaulted(taskInfo, ex).ConfigureAwait(false);
        }
    }
}