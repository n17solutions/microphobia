using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Domain.Clients;
using N17Solutions.Microphobia.Domain.Tasks;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Providers;
using N17Solutions.Microphobia.Utilities.Extensions;
using QueueRunner = N17Solutions.Microphobia.ServiceContract.Models.QueueRunner;
using TaskInfo = N17Solutions.Microphobia.ServiceContract.Models.TaskInfo;

namespace N17Solutions.Microphobia.Data.EntityFramework.Providers
{
    public class DataProvider : IDataProvider
    {
        private readonly TaskContext _context;
        private readonly MicrophobiaConfiguration _config;
        
        public DataProvider(TaskContext context, MicrophobiaConfiguration microphobiaConfiguration)
        {
            _context = context;
            _config = microphobiaConfiguration;
        }

        public async Task Enqueue(TaskInfo task, CancellationToken cancellationToken = default)
        {
            var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId == task.Id, cancellationToken).ConfigureAwait(false);
            if (existingTask == null)
            {
                var tags = new List<string>(task.Tags ?? Enumerable.Empty<string>());
                if (!string.IsNullOrEmpty(_config.Tag))
                    tags.Add(_config.Tag);
                task.Tags = tags.Distinct().ToArray();
                
                task.Status = TaskStatus.Created;
                var domainObject = Domain.Tasks.TaskInfo.FromTaskInfoResponse(task, _config.StorageType);
                await _context.Tasks.AddAsync(domainObject, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existingTask.Status = TaskStatus.Created;
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<TaskInfo> DequeueSingle(string tag = default, CancellationToken cancellationToken = default)
        {
            return (await Dequeue(tag, 1, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<IEnumerable<TaskInfo>> Dequeue(string tag = default, int? limit = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Tasks
                .Where(t => t.Status == TaskStatus.Created);

            query = tag.IsDefault()
                ? query.Where(t => t.Tags.IsDefault())
                : query.Where(t => (t.Tags ?? string.Empty).Contains(tag))
                    .OrderBy(t => t.DateLastUpdated)
                    .ThenBy(t => t.DateCreated);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            var tasks = await query
                .Select(TaskInfoExpressions.ToTaskInfoResponse)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            return tasks;
        }

        public async Task<TaskInfo> GetTaskInfo(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await _context.Tasks
                .Where(t => t.ResourceId == taskId)
                .Select(TaskInfoExpressions.ToTaskInfoResponse)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return task;
        }

        public async Task<IEnumerable<TaskInfo>> GetTasks(DateTime? sinceWhen = null, string tag = default, TaskStatus status = default,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Tasks.AsQueryable();

            if (sinceWhen.HasValue)
                query = query.Where(t => t.DateCreated >= sinceWhen.Value);

            if (!tag.IsDefault())
                query = query.Where(t => (t.Tags ?? string.Empty).Contains(tag));

            if (!status.IsDefault())
                query = query.Where(t => t.Status == status);

            var tasks = await query
                .OrderByDescending(task => task.DateLastUpdated)
                .Select(TaskInfoExpressions.ToTaskInfoResponse)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            return tasks;
        }

        public async Task Save(TaskInfo task, CancellationToken cancellationToken = default)
        {
            var domainObject = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(task.Id), cancellationToken).ConfigureAwait(false);
            domainObject.PopulateFromTaskInfoResponse(task);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> RegisterQueueRunner(QueueRunner queueRunner, CancellationToken cancellationToken = default)
        {
            var tag = queueRunner.Tag ?? _config.Tag;
            var existingRunners = await _context.Runners
                .Where(runner => runner.Name.StartsWith(queueRunner.Name, StringComparison.InvariantCultureIgnoreCase) && runner.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            // Merge the config tag with the queue runner data if necessary.
            queueRunner.Tag = queueRunner.Tag ?? _config.Tag;

            var newRunner = Domain.Clients.QueueRunner.FromQueueRunnerResponse(queueRunner);
            newRunner.UniqueIndexer = existingRunners.Length + 1;

            await _context.Runners.AddAsync(newRunner, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return newRunner.UniqueIndexer;
        }

        public async Task DeregisterQueueRunner(string name, string tag, int uniqueIndexer, CancellationToken cancellationToken = default)
        {
            var runner = await _context.Runners
                .FirstOrDefaultAsync(r => r.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                                          r.Tag.Equals(tag, StringComparison.InvariantCultureIgnoreCase) &&
                                          r.UniqueIndexer.Equals(uniqueIndexer), cancellationToken)
                .ConfigureAwait(false);
            
            if (runner != null)
            {
                _context.Runners.Remove(runner);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DeregisterQueueRunners(IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
            var runners = await _context.Runners.Where(runner => names.Contains(runner.Name)).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            if (!runners.IsNullOrEmpty())
            {
                _context.Runners.RemoveRange(runners);
                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<QueueRunner>> GetRunners(string tag = default, bool isRunning = true, DateTime? lastUpdatedSince = null, DateTime? lastUpdatedBefore = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Runners.AsQueryable();
            query = tag.IsDefault() ? query.Where(runner => string.IsNullOrEmpty(runner.Tag)) : query.Where(runner => runner.Tag == tag);
            query = query.Where(runner => runner.IsRunning == isRunning);

            if (lastUpdatedSince.HasValue || lastUpdatedBefore.HasValue)
                query = query.Where(runner => runner.DateLastUpdated >= (lastUpdatedSince ?? DateTime.UtcNow) || 
                                              runner.DateLastUpdated <= (lastUpdatedBefore ?? DateTime.MinValue));

            return await query.Select(QueueRunnerExpressions.ToQueueRunnerResponse).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task MarkQueueRunnerTaskProcessed(string runnerName, CancellationToken cancellationToken = default)
        {
            var runner = await _context.Runners.FirstOrDefaultAsync(r => r.Name.Equals(runnerName), cancellationToken).ConfigureAwait(false);
            if (runner == null)
                throw new ArgumentException($"No runner could be found with the name '{runnerName}'.");

            runner.LastTaskProcessed = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
