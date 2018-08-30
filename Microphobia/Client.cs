using System;
using System.Diagnostics;
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
        private readonly ISystemLogProvider _systemLogProvider;
        
        private CancellationToken _cancellationToken;
        private uint _nothingToDequeueCount;
        private bool _cancelled;
        
        public Client(IServiceScopeFactory serviceScopeFactory, MicrophobiaConfiguration config, ILogger<Client> logger, ISystemLogProvider systemLogProvider)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _config = config;
            _logger = logger;
            _systemLogProvider = systemLogProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            SetStarted();

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
                        while (true)
                        {
                            if (_cancellationToken.IsCancellationRequested || _cancelled)
                            {
                                await StopAsync(_cancellationToken).ConfigureAwait(false);
                                _cancellationToken.ThrowIfCancellationRequested();
                                
                                break;
                            }

                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var queue = scope.ServiceProvider.GetRequiredService<Queue>();
                                var nextTask = await queue.Dequeue().ConfigureAwait(false);
                                if (nextTask != null)
                                {
                                    _nothingToDequeueCount = 0;

                                    _logger.LogInformation($"Dequeued Task: {nextTask.Id}");
                                    await LogTaskStarted(nextTask, queue).ConfigureAwait(false);

                                    try
                                    {
                                        var stopWatch = new Stopwatch();
                                        stopWatch.Start();

                                        nextTask.Execute(_config.ServiceFactory, _cancellationToken);

                                        stopWatch.Stop();
                                        await LogTaskCompleted(nextTask, stopWatch.Elapsed, queue).ConfigureAwait(false);
                                    }
                                    catch (Exception ex)
                                    {
                                        await LogTaskException(nextTask, ex, queue).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    if (_nothingToDequeueCount++ < _config.StopLoggingNothingToQueueAfter)
                                        _logger.LogInformation("Nothing to Dequeue");
                                }
                            }
                        }
                    }, cancellationToken);
                    
                    task.ContinueWith(t => SetFailure(t.Exception?.InnerException), _cancellationToken, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default)
                        .ContinueWith(async t => await StopAsync(_cancellationToken), _cancellationToken, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);

                    return task;
                });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            SetStopped();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cancelled = true;
        }

        private void SetStarted()
        {
            _cancelled = false;
            _config.IsRunning = true;
            _logger.LogInformation("Microphobia Client is starting");
        }

        private void SetStopped()
        {
            _config.IsRunning = false;
            _logger.LogInformation("Microphobia Client is stopping.");
        }
        
        private async Task SetFailure(Exception exception)
        {
            var logEntryId = await _systemLogProvider.Log(new SystemLog
            {
                Message = exception.Message,
                Source = exception.Source,
                StackTrace = exception.StackTrace,
                Level = LogLevel.Error,
                Data = exception
            }, _config.StorageType).ConfigureAwait(false);
            
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