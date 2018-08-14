using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using N17Solutions.Microphobia.Domain.Model;
using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.Utilities.Extensions;
using N17Solutions.Microphobia.Utilities.Serialization;

namespace N17Solutions.Microphobia.Domain.Tasks
{
    public class TaskInfo : AggregateRoot 
    {
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
        /// The Type of the return object from the method.
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// The current status of this task.
        /// </summary>
        public TaskStatus Status { get; set; } = TaskStatus.Created;
        
        /// <summary>
        /// If the task Faulted, information as to why.
        /// </summary>
        public string FailureDetails { get; set; }
        
        /// <summary>
        /// A serialised representation of this Task
        /// </summary>
        public string Data { get; set; }
        
        /// <summary>
        /// Whether the Task is asynchronous or not.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Transforms the <see cref="Data" /> property of a <see cref="TaskInfo" /> domain model into a <see cref="ServiceContract.Models.TaskInfo" />
        /// </summary>
        /// <returns>The resultant model.</returns>
        public ServiceContract.Models.TaskInfo ToTaskInfoResponse()
        {
            return TaskInfoSerialization.Deserialize(Data);
        }
        
        /// <summary>
        /// Populates this Task object with the details from the <see cref="ServiceContract.Models.TaskInfo" /> parameter.
        /// </summary>
        /// <param name="response">The <see cref="ServiceContract.Models.TaskInfo" /> to populate from.</param>
        public void PopulateFromTaskInfoResponse(ServiceContract.Models.TaskInfo response)
        {
            AssemblyName = response.AssemblyName;
            TypeName = response.TypeName;
            MethodName = response.MethodName;
            ReturnType = response.ReturnType.FullName;
            Status = response.Status;
            DateCreated = response.DateCreated;
            DateLastUpdated = response.DateLastUpdated;
            FailureDetails = response.FailureDetails;
            IsAsync = response.IsAsync;
            Data = TaskInfoSerialization.Serialize(response);
        }

        /// <summary>
        /// Transforms a <see cref="ServiceContract.Models.TaskInfo" /> model into a <see cref="TaskInfo" /> domain model.
        /// </summary>
        /// <param name="response">The response to transform.</param>
        /// <returns>The resultant domain model.</returns>
        public static TaskInfo FromTaskInfoResponse(ServiceContract.Models.TaskInfo response)
        {
            var taskInfo = new TaskInfo
            {
                ResourceId = response.Id.IsDefault() ? Guid.NewGuid() : response.Id,
                AssemblyName = response.AssemblyName,
                TypeName = response.TypeName,
                MethodName = response.MethodName,
                ReturnType = response.ReturnType.FullName,
                Status = response.Status,
                FailureDetails = response.FailureDetails,
                IsAsync = response.IsAsync
            };

            // Why don't we do this in the object initializer?
            // Because the dates haven't been populated, until the TaskInfo has been created!
            response.DateCreated = taskInfo.DateCreated;
            response.DateLastUpdated = taskInfo.DateLastUpdated;
            taskInfo.Data = TaskInfoSerialization.Serialize(response);

            return taskInfo;
        }
    }

    public static class TaskInfoExpressions
    {
        public static Expression<Func<TaskInfo, ServiceContract.Models.TaskInfo>> ToTaskInfoResponse =>
            domainModel =>
                TaskInfoSerialization.Deserialize(domainModel.Data);
    }
}