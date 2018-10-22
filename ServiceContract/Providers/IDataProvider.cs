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
        Task Enqueue(TaskInfo task, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the next Task to Execute.
        /// </summary>
        /// <param name="tag">Any tag to use in the dequeue query.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>A <see cref="TaskInfo" /> representing the dequeued task.</returns>
        Task<TaskInfo> DequeueSingle(string tag = default, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the next Tasks to Execute.
        /// </summary>
        /// <param name="tag">Any tag to use in the dequeue query.</param>
        /// <param name="limit">If we want to limit the amount of tasks to dequeue.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>A collection of <see cref="TaskInfo" /> representing the dequeued tasks.</returns>
        Task<IEnumerable<TaskInfo>> Dequeue(string tag = default, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the Task with the given unique identifier.
        /// </summary>
        /// <param name="taskId">The globally unique identifier to use to fetch the Task.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>The <see cref="TaskInfo" /> found with the given identifier.</returns>
        Task<TaskInfo> GetTaskInfo(Guid taskId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all Tasks in the system matching the given filters.
        /// </summary>
        /// <param name="sinceWhen">If provided, only tasks created since this date will be retrieved.</param>
        /// <param name="tag">If provided, only tasks with the given tag will be retrieved.</param>
        /// <param name="status">If provided, only tasks with the given status will be retrieved.></param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>A collection of <see cref="TaskInfo" /> objects found that match the given filters.</returns>
        Task<IEnumerable<TaskInfo>> GetTasks(DateTime? sinceWhen = null, string tag = default, TaskStatus status = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a Task instance.
        /// </summary>
        /// <param name="task">The task to save.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <remarks>Will not Enqueue, only updates Tasks.</remarks>
        /// <example>To update a status</example>
        Task Save(TaskInfo task, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a Queue Runner as part of the system.
        /// </summary>
        /// <param name="runner">The runner to register.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>The unique indexer of this Queue Runner</returns> 
        Task<int> RegisterQueueRunner(QueueRunner runner, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// De-registers a Queue Runner from the system.
        /// </summary>
        /// <param name="name">The name of the runner to de-register.</param>
        /// <param name="tag">The tag that this runner was assigned.</param>
        /// <param name="uniqueIndexer">The indexer of the runner to de-register.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        Task DeregisterQueueRunner(string name, string tag, int uniqueIndexer, CancellationToken cancellationToken = default);

        /// <summary>
        /// De-registers multiple Queue Runners from the system.
        /// </summary>
        /// <param name="names">A collection of the runner names to de-register.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        Task DeregisterQueueRunners(IEnumerable<string> names, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the Queue Runner with the given name as having processed a task.
        /// </summary>
        /// <param name="runnerName">The name of the Queue Runner to mark.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        Task MarkQueueRunnerTaskProcessed(string runnerName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all the runners currently registered.
        /// </summary>
        /// <param name="tag">Only gets runners with the assigned tag.</param>
        /// <param name="isRunning">Denotes whether to only retrieve Runners that are currently running or not.</param>
        /// <param name="lastUpdatedSince">If provided, will only retrieve Runners that have marked updated since the date provided.</param>
        /// <param name="lastUpdatedBefore">If provided, will only retrieve Runners that have marked last updated before the date provided.</param>
        /// <param name="cancellationToken">Any <see cref="CancellationToken" /> to use alongside this request.</param>
        /// <returns>A collection of <see cref="QueueRunner" /> objects that match the given filters.</returns>
        Task<IEnumerable<QueueRunner>> GetRunners(string tag = default, bool isRunning = true, DateTime? lastUpdatedSince = null, DateTime? lastUpdatedBefore = null,
            CancellationToken cancellationToken = default);
    }
}
