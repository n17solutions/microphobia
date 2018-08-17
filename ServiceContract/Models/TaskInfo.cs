using System;
using System.Threading.Tasks;
using N17Solutions.Microphobia.ServiceContract.Serialization;
using Newtonsoft.Json;

namespace N17Solutions.Microphobia.ServiceContract.Models
{
    public class TaskInfo
    {
        /// <summary>
        /// The Unique Identifier of this Task.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The name of the assembly this Task will be executed within.
        /// </summary>
        public string AssemblyName { get; set; }
        
        /// <summary>
        /// The name of the Type this Task belongs to.
        /// </summary>
        public string TypeName { get; set; }
        
        /// <summary>
        /// The name of the executing method.
        /// </summary>
        public string MethodName { get; set; }
        
        /// <summary>
        /// Any arguments being passed to the method during execution.
        /// </summary>
        public object[] Arguments { get; set; }
        
        /// <summary>
        /// The Type of the return object from the method.
        /// </summary>
        public Type ReturnType { get; set; }

        /// <summary>
        /// The Status this task is currently in.
        /// </summary>
        public TaskStatus Status { get; set; }
        
        /// <summary>
        /// If the Task Faulted, any information as to why.
        /// </summary>
        public string FailureDetails { get; set; }
        
        /// <summary>
        /// The Date this Task was created.
        /// </summary>
        public DateTime DateCreated { get; set; }
        
        /// <summary>
        /// The Date this Task was last updated.
        /// </summary>
        public DateTime DateLastUpdated { get; set; }
    }
}