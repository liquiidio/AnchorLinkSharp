using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnchorLinkSharp
{
    /** @internal */
    public class SerializedLinkSession
    {
        public string Type { get ; set; }
        public Dictionary<string, object> Metadata { get; set; }//: {[key: string]: any}

        [JsonConverter(typeof(LinkSessionDataConverter))]
        public LinkSessionDataBase Data { get; set; } //data: any
    }
}