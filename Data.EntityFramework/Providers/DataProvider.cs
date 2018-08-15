using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using N17Solutions.Microphobia.Data.EntityFramework.Contexts;
using N17Solutions.Microphobia.Domain.Tasks;
using N17Solutions.Microphobia.ServiceContract.Configuration;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.ServiceContract.Providers;
using TaskInfo = N17Solutions.Microphobia.ServiceContract.Models.TaskInfo;

namespace N17Solutions.Microphobia.Data.EntityFramework.Providers
{
    public class DataProvider : IDataProvider
    {
        private readonly TaskContext _context;
        private readonly Storage _storageType;

        public DataProvider(TaskContext context, MicrophobiaConfiguration microphobiaConfiguration)
        {
            _context = context;
            _storageType = microphobiaConfiguration.StorageType;
        }

        public async Task Enqueue(TaskInfo task, CancellationToken cancellationToken = default(CancellationToken))
        {
            var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId == task.Id, cancellationToken).ConfigureAwait(false);
            if (existingTask == null)
            {
                task.Status = TaskStatus.Created;
                var domainObject = Domain.Tasks.TaskInfo.FromTaskInfoResponse(task, _storageType);
                await _context.Tasks.AddAsync(domainObject, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existingTask.Status = TaskStatus.Created;
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<TaskInfo> Dequeue(CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = await _context.Tasks
                .Where(t => t.Status == TaskStatus.Created)
                .OrderBy(t => t.DateLastUpdated)
                .ThenBy(t => t.DateCreated)
                .Select(TaskInfoExpressions.ToTaskInfoResponse)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return task;
        }

        public async Task<TaskInfo> GetTaskInfo(Guid taskId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = await _context.Tasks
                .Where(t => t.ResourceId == taskId)
                .Select(TaskInfoExpressions.ToTaskInfoResponse)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return task;
        }

        public async Task<IEnumerable<TaskInfo>> GetTasks(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tasks = await _context.Tasks.Select(TaskInfoExpressions.ToTaskInfoResponse).ToArrayAsync(cancellationToken).ConfigureAwait(false);
            return tasks;
        }

        public async Task Save(TaskInfo task, CancellationToken cancellationToken = default(CancellationToken))
        {
            var domainObject = await _context.Tasks.FirstOrDefaultAsync(t => t.ResourceId.Equals(task.Id), cancellationToken).ConfigureAwait(false);
            domainObject.PopulateFromTaskInfoResponse(task);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
