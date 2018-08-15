using Microsoft.Extensions.Logging;
using N17Solutions.Microphobia.Domain.Model;
using N17Solutions.Microphobia.ServiceContract.Enums;
using N17Solutions.Microphobia.Utilities.Identifiers;
using Newtonsoft.Json;

namespace N17Solutions.Microphobia.Domain.Logs
{
    public class SystemLog : AggregateRoot
    {
        /// <summary>
        /// The Log Message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Any Stack Trace associated with this log
        /// </summary>
        public string StackTrace { get; set; }
        
        /// <summary>
        /// The source of this log
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// The <see cref="LogLevel" /> of this log.
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// Any extra data to add to this log
        /// </summary>
        public string Data { get; set; }
        
        /// <summary>
        /// Transforms a <see cref="ServiceContract.Models.SystemLog" /> model into a <see cref="SystemLog" /> domain model.
        /// </summary>
        /// <param name="response">The response to transform.</param>
        /// <param name="storageType">The storage mechanism currently in use - helps generate the correct resource id</param>
        /// <returns>The resultant domain model.</returns>
        public static SystemLog FromSystemLogResponse(ServiceContract.Models.SystemLog response, Storage storageType)
        {
            var systemLog = new SystemLog
            {
                ResourceId = SequentialGuidGenerator.Generate(storageType),
                Message = response.Message,
                Source = response.Source,
                StackTrace = response.StackTrace,
                Level = response.Level,
                Data = JsonConvert.SerializeObject(response.Data)
            };

            return systemLog;
        }
    }
}