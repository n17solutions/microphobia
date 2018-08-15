using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using N17Solutions.Microphobia.ServiceContract;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;

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
        
        //private static readonly object Lock = new object();
        
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
            SetStarted();
            
            var task = Task.Run(async () =>
            {
                // If already cancelled
                _cancellationToken.ThrowIfCancellationRequested();

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
                        Console.WriteLine($"Dequeued Task: {nextTask.Id}");

                        var hasScopedServiceFactory = _config.ScopedServiceFactories.ContainsKey(nextTask.Id);
                        await LogTaskStarted(nextTask).ConfigureAwait(false);

                        try
                        {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();

                            var serviceFactory = hasScopedServiceFactory
                                ? _config.ScopedServiceFactories[nextTask.Id]
                                : _config.ServiceFactory;

                            if (nextTask.IsAsync)
                                await nextTask.ExecuteAsync(serviceFactory);
                            else
                                nextTask.Execute(serviceFactory);

                            stopwatch.Stop();
                            await LogTaskCompleted(nextTask, stopwatch.Elapsed).ConfigureAwait(false);

                            if (hasScopedServiceFactory)
                                _config.ScopedServiceFactories.TryRemove(nextTask.Id, out var _);
                        }
                        catch (Exception e)
                        {
                            await LogTaskException(nextTask, e).ConfigureAwait(false);
                        }
                    }
                    
                    Thread.Sleep(_config.PollIntervalMs);
                }
            }, _cancellationTokenSource.Token);

            task.ContinueWith(SetFailure, _cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default)
                .ContinueWith(t => SetStopped(), _cancellationTokenSource.Token, TaskContinuationOptions.OnlyOnCanceled, TaskScheduler.Default);
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
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

        private async Task SetFailure(Task t)
        {
            if (t.Exception != null && t.Exception.InnerExceptions.Any())
            {
                foreach (var exception in t.Exception.InnerExceptions)
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