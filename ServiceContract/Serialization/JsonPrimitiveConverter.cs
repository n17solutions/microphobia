using System;
using System.ComponentModel.Design.Serialization;
using Newtonsoft.Json;

namespace N17Solutions.Microphobia.ServiceContract.Serialization
{
    public class JsonPrimitiveConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanConvert(Type objectType) => objectType.IsPrimitive || objectType.IsAssignableFrom(typeof(Guid));

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //return objectType.IsAssignableFrom(typeof(Guid)) ? serializer.Deserialize<Guid>(reader) : existingValue;
            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (serializer.TypeNameHandling)
            {
                case TypeNameHandling.All:
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type", false);

                    switch (serializer.TypeNameAssemblyFormatHandling)
                    {
                        case TypeNameAssemblyFormatHandling.Full:
                            writer.WriteValue(value.GetType().AssemblyQualifiedName);
                            break;
                        default:
                            writer.WriteValue(value.GetType().FullName);
                            break;
                    }
                    
                    writer.WritePropertyName("$value", false);
                    writer.WriteValue(value);
                    writer.WriteEndObject();
                    break;
                
                default:
                    writer.WriteValue(value);
                    break;
            }
        }
        
        
    }
}