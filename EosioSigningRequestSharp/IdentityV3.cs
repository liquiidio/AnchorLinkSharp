using Newtonsoft.Json;
using EosSharp;

namespace EosioSigningRequest
{
    public class IdentityV3 : IdentityV2
    {
        [JsonProperty("scope")]
        //! Scope Requested
        public string Scope { get; set; }
    }
}