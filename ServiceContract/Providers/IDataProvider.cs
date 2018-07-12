using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Models;

namespace N17Solutions.Microphobia.ServiceContract.Providers
{
    public interface IDataProvider
    {
        /// <summary>
        /// Enqueues the given task.
        /// </summary>
        /// <param name="task">The task to Enqueue.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <remarks>Will enqueue with a status of <see cref="TaskStatus.Created" /> regardless of the status given. This is an enqueue action not a save action.</remarks>
        Task Enqueue(TaskInfo task, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the next Task to Execute.
        /// </summary>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns><see cref="TaskInfo" /> representing the dequeued task.</returns>
        Task<TaskInfo> Dequeue(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the Task with the given unique identifier.
        /// </summary>
        /// <param name="taskId">The globally unique identifier to use to fetch the Task.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>The <see cref="TaskInfo" /> found with the given identifier.</returns>
        Task<TaskInfo> GetTaskInfo(Guid taskId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets all Tasks in the system matching the given filters.
        /// </summary>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>A collection of <see cref="TaskInfo" /> objects found that match the given filters.</returns>
        Task<IEnumerable<TaskInfo>> GetTasks(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Saves a Task instance.
        /// </summary>
        /// <param name="task">The task to save.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <remarks>Will not Enqueue, only updates Tasks.</remarks>
        /// <example>To update a status</example>
        Task Save(TaskInfo task, CancellationToken cancellationToken = default(CancellationToken));
    }
}
