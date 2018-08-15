using Microsoft.Extensions.Logging;

namespace N17Solutions.Microphobia.ServiceContract.Models
{
    public class SystemLog
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
        /// The <see cref="LogLevel" /> of this log
        /// </summary>
        public LogLevel Level { get; set; }
        
        /// <summary>
        /// Any extra data to add to this log
        /// </summary>
        public object Data { get; set; }
    }
}