using System;

namespace N17Solutions.Microphobia.ServiceContract.Models
{
    public class QueueRunner
    {
        /// <summary>
        /// The Name of the runner
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Denotes whether this Queue Runner is actually running
        /// </summary>
        public bool IsRunning { get; set; }
        
        /// <summary>
        /// The date and time when this runner last processed a task
        /// </summary>
        public DateTime? LastTaskProcessed { get; set; }

        /// <summary>
        /// The Date this Runner was registered
        /// </summary>
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    }
}