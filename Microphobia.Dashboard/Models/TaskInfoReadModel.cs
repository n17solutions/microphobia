using System;
using Humanizer;
using N17Solutions.Microphobia.ServiceContract.Models;

namespace N17Solutions.Microphobia.Dashboard.Models
{
    public class TaskInfoReadModel
    {
        public Guid Id { get; }
        public string AssemblyName { get; }
        public string TypeName { get; }
        public string MethodName { get; }
        public string Status { get; }
        public uint StatusId { get; }
        public string Arguments { get; }
        public string DateCreated { get; }
        public string DateLastUpdated { get; }
        public string FailureDetails { get; }
        public string Tags { get; }

        public TaskInfoReadModel(TaskInfo taskInfo)
        {
            Id = taskInfo.Id;
            AssemblyName = taskInfo.AssemblyName;
            TypeName = taskInfo.TypeName;
            MethodName = taskInfo.MethodName;
            Status = taskInfo.Status.Humanize().Transform(To.TitleCase);
            StatusId = (uint) taskInfo.Status;
            Arguments = string.Join(", ", taskInfo.Arguments);
            DateCreated = taskInfo.DateCreated.Humanize();
            DateLastUpdated = taskInfo.DateLastUpdated.Humanize();
            FailureDetails = taskInfo.FailureDetails;
            Tags = taskInfo.Tags == null ? null : string.Join(", ", taskInfo.Tags);
        }
    }
}