using System;
using System.IO;
using Newtonsoft.Json;

namespace AnchorLinkSharp
{
    public class LinkSessionDataConverter : JsonConverter<LinkSessionDataBase>
    {
        private class SerializableLinkSessionWrapper
        {
            [JsonProperty("type")]
            public string Type;

            [JsonProperty("data")]
            public string Data;

#if Unity
            [Preserve]
#endif
            public SerializableLinkSessionWrapper()
            {

            }

            public SerializableLinkSessionWrapper(LinkSessionDataBase data)
            {
                Type = data.GetType().Name;
                Data = JsonConvert.SerializeObject(data);
            }
        }
        public override void WriteJson(JsonWriter writer, LinkSessionDataBase value, JsonSerializer serializer)
        {
            writer.WriteValue(JsonConvert.SerializeObject(new SerializableLinkSessionWrapper(value)));
        }

        public override LinkSessionDataBase ReadJson(JsonReader reader, Type objectType, LinkSessionDataBase existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var serializableLinkSessionWrapper = JsonConvert.DeserializeObject<SerializableLinkSessionWrapper>(reader.Value.ToString());
            if (serializableLinkSessionWrapper.Type == "LinkChannelSessionData")
                return JsonConvert.DeserializeObject<LinkChannelSessionData>(serializableLinkSessionWrapper.Data);
            if(serializableLinkSessionWrapper.Type == "LinkFallbackSessionData")
                return JsonConvert.DeserializeObject<LinkFallbackSessionData>(serializableLinkSessionWrapper.Data);
            throw new InvalidDataException($"Unknown SerializableLinkSessionWrapper.Data of type {serializableLinkSessionWrapper.Type}");
        }
    }
}