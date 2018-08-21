using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.ServiceContract;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Utilities.Locking;
using Polly;

namespace N17Solutions.Microphobia
{
    public class Client
    {
        private readonly Queue _queue;
        private readonly MicrophobiaConfiguration _config;
        private readonly ILogger _logger;
        private readonly ISystemLogProvider _systemLogProvider;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;
        
        private readonly AsyncLock _lock = new AsyncLock();
        
        private int _nothingToDequeueCount;
        
        public Client(Queue queue, MicrophobiaConfiguration config, ILoggerFactory loggerFactory, ISystemLogProvider systemLogProvider)
        {
            _queue = queue;
            _config = config;
            _logger = loggerFactory.CreateLogger("Microphobia.Logger");
            _systemLogProvider = systemLogProvider;

            _cancellationToken = _cancellationTokenSource.Token;
        }

        public void Start()
        {
            Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .WaitAndRetry(new[] {TimeSpan.FromMilliseconds(_config.PollIntervalMs * 3)}, async (exception, timeSpan) =>
                {
                    await SetFailure(exception).ConfigureAwait(false);
                    await Restart().ConfigureAwait(false);
                })
                .Execute(() =>
                {
                    SetStarted();
            
                    var task = Task.Run(async () =>
                    {
                        // If already cancelled
                        _cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            while (true)
                            {
                                if (_cancellationToken.IsCancellationRequested)
                                {
                                    SetStopped();
                                    _cancellationToken.ThrowIfCancellationRequested();
                                }

                                var nextTask = await _queue.Dequeue().ConfigureAwait(false);

                                if (nextTask != null)
                                {
                                    await ExecuteTask(nextTask).ConfigureAwait(false);
                                }
                                else
                                {
                                    if (_nothingToDequeueCount++ >= _config.StopLoggingNothingToQueueAfter)
                                        continue;

                                    Console.WriteLine("Nothing to Dequeue");
                                }

                                if (!_cancellationToken.IsCancellationRequested)
                                    Task.Delay(_config.PollIntervalMs, _cancellationToken).Wait(_cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            await SetFailure(ex).ConfigureAwait(false);
                    
                        }
                    }, _cancellationTokenSource.Token);

                    task.ContinueWith(SetFailure, _cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default)
                        .ContinueWith(t => SetStopped(), _cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
                });
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task ExecuteTask(TaskInfo task)
        {
            using (var _ = await _lock.Lock())
            {
                _nothingToDequeueCount = 0;
                Console.WriteLine($"Dequeued Task: {task.Id}");
                await LogTaskStarted(task).ConfigureAwait(false);

                try
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    task.Execute(_config.ServiceFactory, _cancellationToken);

                    stopWatch.Stop();
                    await LogTaskCompleted(task, stopWatch.Elapsed).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await LogTaskException(task, ex).ConfigureAwait(false);
                }
            }
        }

        private void SetStarted()
        {
            _config.IsRunning = true;
            
            Console.WriteLine("Microphobia Client Started...");
        }

        private void SetStopped()
        {
            _config.IsRunning = false;
            
            Console.WriteLine("Microphobia Client Stopped.");
        }

        private async Task Restart()
        {
            var logEntryId = await _systemLogProvider.Log(new SystemLog
            {
                Message = "Restarting Microphobia Client",
                Level = LogLevel.Information
            }, _config.StorageType).ConfigureAwait(false);
            
            Console.WriteLine($"Restarting Microphobia Client. This has been logged to the System Log with Resource Id {logEntryId}");
            
            Stop();
            Start();
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
            
            Console.WriteLine($"An error occurred while running the Microphobia Client and has been logged to the System Log with Resource Id: {logEntryId}");
        }

        private async Task SetFailure(Task t)
        {
            if (t.Exception != null && t.Exception.InnerExceptions.Any())
            {
                foreach (var exception in t.Exception.InnerExceptions)
                {
                    await SetFailure(exception).ConfigureAwait(false);
                }
            }
            else
            {
                Console.WriteLine($"An unknown error occurred while running the Microphobia Client: {t.Exception?.Message}");
            }

            SetStopped();
        }

        private async Task LogTaskException(TaskInfo taskInfo, Exception ex)
        {
            var logMessage = Messages.TaskThrewException(taskInfo);
            _logger.Log(LogLevel.Error, ex, logMessage);

            await _queue.SetQueuedTaskFaulted(taskInfo, ex).ConfigureAwait(false);
        }

        private async Task LogTaskStarted(TaskInfo taskInfo)
        {
            var logMessage = Messages.TaskStarted(taskInfo);
            _logger.Log(LogLevel.Trace, logMessage);

            await _queue.SetQueuedTaskRunning(taskInfo);
        }

        private async Task LogTaskCompleted(TaskInfo taskInfo, TimeSpan completionTime)
        {
            var logMessage = Messages.TaskFinished(taskInfo, completionTime);
            _logger.Log(LogLevel.Information, logMessage);

            await _queue.SetQueuedTaskCompleted(taskInfo);
        }
    }
}