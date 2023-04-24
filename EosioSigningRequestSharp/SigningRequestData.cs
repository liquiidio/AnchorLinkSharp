using System.Collections.Generic;
using Newtonsoft.Json;

namespace EosioSigningRequest
{
    public class SigningRequestData
    {
        [JsonProperty("chain_id")]
        //! Hash representing the ID of the chain.
        public KeyValuePair<string, object> ChainId { get; set; }

        [JsonProperty("req")]
        //! The actual Request
        public KeyValuePair<string, object> Req { get; set; }

        [JsonProperty("flags")]
        //! Flags set for this Request (Broadcast, Background etc.)
        public byte Flags { get; set; }

        [JsonProperty("callback")]
        //! THe Callback Url
        public string Callback { get; set; }

        [JsonProperty("info")]
        //! SigningRequest Metadata
        public List<object> Info { get; set; }
    }
}