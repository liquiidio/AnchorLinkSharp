using Newtonsoft.Json;
using EosSharp;

namespace EosioSigningRequest
{
    public class IdentityV2
    {
        [JsonProperty("permission")]
        //! Permission Requested
        public EosSharp.Core.Api.v1.PermissionLevel Permission { get; set; }
    }
}