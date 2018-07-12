using System;
using System.Text;
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
        public string DateCreated { get; }
        public string FailureDetails { get; }

        public TaskInfoReadModel(TaskInfo taskInfo)
        {
            Id = taskInfo.Id;
            AssemblyName = taskInfo.AssemblyName;
            TypeName = taskInfo.TypeName;
            MethodName = taskInfo.MethodName;
            Status = taskInfo.Status.Humanize().Transform(To.TitleCase);
            StatusId = (uint) taskInfo.Status;
            DateCreated = taskInfo.DateCreated.Humanize();
            FailureDetails = taskInfo.FailureDetails;
        }

        private string EnvironmentToHtml(string input)
        {
            var htmlBuilder = new StringBuilder(input);
            htmlBuilder.Replace(Environment.NewLine, "<br />");

            return htmlBuilder.ToString();
        }
    }
}