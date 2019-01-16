using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        
        public Queue(IDataProvider dataProvider, MicrophobiaHubContext taskHubContext, IServiceScopeFactory serviceScopeFactory)
        {
            _dataProvider = dataProvider;
            _taskHubContext = taskHubContext;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task Enqueue(Expression<Action> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public async Task Enqueue(IEnumerable<Expression<Action>> expressions, IEnumerable<string> tags = default)
        {
            var tasks = expressions.Select(expression => expression.ToTaskInfo(tags));
            foreach (var task in tasks)
                await Enqueue(task);
        }
        
        public Task Enqueue<TExecutor>(Expression<Action<TExecutor>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public async Task Enqueue<TExecutor>(IEnumerable<Expression<Action<TExecutor>>> expressions, IEnumerable<string> tags = default)
        {
            var tasks = expressions.Select(expression => expression.ToTaskInfo(tags));
            foreach (var task in tasks)
                await Enqueue(task);
        }

        public Task Enqueue(Expression<Func<Task>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public async Task Enqueue(IEnumerable<Expression<Func<Task>>> expressions, IEnumerable<string> tags = default)
        {
            var tasks = expressions.Select(expression => expression.ToTaskInfo(tags));
            foreach (var task in tasks)
                await Enqueue(task);
        }
        
        public Task Enqueue<TExecutor>(Expression<Func<TExecutor, Task>> expression, IEnumerable<string> tags = default)
        {
            var taskInfo = expression.ToTaskInfo(tags);
            return Enqueue(taskInfo);
        }

        public async Task Enqueue<TExecutor>(IEnumerable<Expression<Func<TExecutor, Task>>> expressions, IEnumerable<string> tags = default)
        {
            var tasks = expressions.Select(expression => expression.ToTaskInfo(tags));
            foreach (var task in tasks)
                await Enqueue(task);
        }
        
        public async Task<TaskInfo> DequeueSingle(string tag = default, CancellationToken cancellationToken = default)
        {
            var dequeuedTask = await _dataProvider.DequeueSingle(tag, cancellationToken).ConfigureAwait(false);
            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var taskProcessor = scope.ServiceProvider.GetRequiredService<TaskProcessor>();
                await taskProcessor.SetQueuedTaskWaitingForRun(dequeuedTask);
            }

            return dequeuedTask;
        }
        
        public async Task<IEnumerable<TaskInfo>> Dequeue(string tag = default, int? limit = null, CancellationToken cancellationToken = default)
        {
            var dequeuedTasks = await _dataProvider.Dequeue(tag, limit, cancellationToken).ConfigureAwait(false);
            var taskInfos = dequeuedTasks as TaskInfo[] ?? dequeuedTasks.ToArray();
            
            foreach (var task in taskInfos)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var taskProcessor = scope.ServiceProvider.GetRequiredService<TaskProcessor>();
                    await taskProcessor.SetQueuedTaskWaitingForRun(task);
                }
            }

            return taskInfos;
        }

        private async Task Enqueue(TaskInfo taskInfo)
        {
            taskInfo.Status = TaskStatus.Created;
            
            await _dataProvider.Enqueue(taskInfo).ConfigureAwait(false);
            await _taskHubContext.RefreshTasks().ConfigureAwait(false);
        }
    }
}