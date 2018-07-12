using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Humanizer;
using N17Solutions.Microphobia.ServiceContract.Models;

namespace N17Solutions.Microphobia.ServiceContract
{
    public static class Messages
    {
        private static readonly CultureInfo CurrentCulture;

        static Messages()
        {
            CurrentCulture = Thread.CurrentThread.CurrentCulture;
        }

        public static string TaskThrewException(TaskInfo taskInfo)
        {
            var messages = new Dictionary<string, string>
            {
                { "en-gb", "[{0}] Task Error: {1}.{2} threw an exception. (Task Id: {3})"}
            };

            return CreateMessage(taskInfo, messages);
        }

        public static string TaskStarted(TaskInfo taskInfo)
        {
            var messages = new Dictionary<string, string>
            {
                { "en-gb", "[{0}] Task Started Processing: {1}.{2} (Task Id: {3})"}
            };

            return CreateMessage(taskInfo, messages);
        }

        public static string TaskFinished(TaskInfo taskInfo, TimeSpan completionTime)
        {
            var messages = new Dictionary<string, string>
            {
                {"en-gb", "[{0}] Task Finished Processing: {1}.{2} (Task Id: {3}) - {4}"}
            };

            return CreateMessage(taskInfo, messages, completionTime.Humanize(3, CurrentCulture));
        }

        private static string CreateMessage(TaskInfo taskInfo, Dictionary<string, string> messages, params string[] extras)
        {
            if (!messages.TryGetValue(CurrentCulture.Name.ToLowerInvariant(), out var message))
                message = messages[messages.Keys.First()];

            var stringParams = new List<object>(extras);
            stringParams.Insert(0, DateTime.UtcNow.ToString("R"));
            stringParams.Insert(1, taskInfo.AssemblyName);
            stringParams.Insert(2, taskInfo.MethodName);
            stringParams.Insert(3, taskInfo.Id);

            return string.Format(message, stringParams.ToArray());
        }
    }
}
