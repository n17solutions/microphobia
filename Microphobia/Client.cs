using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.Configuration;
using N17Solutions.Microphobia.ServiceContract;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.Utilities.Extensions;

namespace N17Solutions.Microphobia
{
    public class Client
    {
        private bool _cancelled;

        private readonly Queue _queue;
        private readonly MicrophobiaConfiguration _config;
        private readonly ILogger _logger;
        
        private static readonly object Lock = new object();

        public Client(Queue queue, MicrophobiaConfiguration config, ILoggerFactory loggerFactory)
        {
            _queue = queue;
            _config = config;
            _logger = loggerFactory.CreateLogger("Microphobia.Logger");
        }

        public void Start()
        {
            lock (Lock)
                SetStarted();
            
            Task.Run(async () =>
            {
                while (true)
                {
                    lock (Lock)
                    {
                        if (_cancelled)
                        {
                            SetStopped();
                            return;
                        }
                    }

                    var nextTask = await _queue.Dequeue().ConfigureAwait(false);
                    if (nextTask != null)
                    {
                        var hasScopedServiceFactory = _config.ScopedServiceFactories.ContainsKey(nextTask.Id);
                        await LogTaskStarted(nextTask).ConfigureAwait(false);

                        try
                        {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();

                            var serviceFactory = hasScopedServiceFactory
                                ? _config.ScopedServiceFactories[nextTask.Id]
                                : _config.ServiceFactory;
                            
                            nextTask.Execute(serviceFactory);

                            stopwatch.Stop();
                            await LogTaskCompleted(nextTask, stopwatch.Elapsed).ConfigureAwait(false);

                            if (hasScopedServiceFactory)
                                _config.ScopedServiceFactories.TryRemove(nextTask.Id, out var sf);
                        }
                        catch (Exception e)
                        {
                            await LogTaskException(nextTask, e).ConfigureAwait(false);
                        }
                    }

                    Thread.Sleep(_config.PollIntervalMs);
                }
            });
        }

        public void Stop()
        {
            lock (Lock)
            {
                _cancelled = true;
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
            _cancelled = false;
            
            Console.WriteLine("Microphobia Client Stopped.");
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