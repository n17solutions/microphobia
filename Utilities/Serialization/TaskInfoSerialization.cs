using N17Solutions.Microphobia.ServiceContract.Models;
using N17Solutions.Microphobia.ServiceContract.Serialization;
using Newtonsoft.Json;

namespace N17Solutions.Microphobia.Utilities.Serialization
{
    public static class TaskInfoSerialization
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        static TaskInfoSerialization()
        {
            SerializerSettings.Converters.Insert(0, new JsonPrimitiveConverter());
        }
        
        public static string Serialize(TaskInfo taskInfo)
        {
            return JsonConvert.SerializeObject(taskInfo, SerializerSettings);
        }

        public static TaskInfo Deserialize(string json)
        {
            return string.IsNullOrEmpty(json) 
                ? null 
                : JsonConvert.DeserializeObject<TaskInfo>(json, SerializerSettings);            
        }
    }
}