using System;
using System.Linq.Expressions;
using N17Solutions.Microphobia.Domain.Model;

namespace N17Solutions.Microphobia.Domain.Clients
{
    public class QueueRunner : AggregateRoot
    {
        /// <summary>
        /// The Name of the runner
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Any tags this runner is processing
        /// </summary>
        public string Tag { get; set; }
        
        /// <summary>
        /// Denotes whether this Queue Runner is actually running
        /// </summary>
        public bool IsRunning { get; set; }
        
        /// <summary>
        /// The date and time when this runner last processed a task
        /// </summary>
        public DateTime? LastTaskProcessed { get; set; }
        
        /// <summary>
        /// If there are multiple runners with the same name, this is used to differentiate
        /// </summary>
        public int UniqueIndexer { get; set; }
        
        /// <summary>
        /// The Date this Runner was registered
        /// </summary>
        public DateTime DateRegistered { get; set; }

        public static QueueRunner FromQueueRunnerResponse(ServiceContract.Models.QueueRunner runner)
        {
            var queueRunner = new QueueRunner
            {
                Name = runner.Name,
                IsRunning = runner.IsRunning,
                LastTaskProcessed = runner.LastTaskProcessed,
                DateRegistered = runner.DateRegistered,
                Tag = runner.Tag
            };

            return queueRunner;
        }
    }

    public static class QueueRunnerExpressions
    {
        public static Expression<Func<QueueRunner, ServiceContract.Models.QueueRunner>> ToQueueRunnerResponse =>
            domainModel =>
                new ServiceContract.Models.QueueRunner
                {
                    Name = domainModel.Name,
                    IsRunning = domainModel.IsRunning,
                    LastTaskProcessed = domainModel.LastTaskProcessed,
                    DateRegistered = domainModel.DateRegistered,
                    Tag = domainModel.Tag
                };
    }
}