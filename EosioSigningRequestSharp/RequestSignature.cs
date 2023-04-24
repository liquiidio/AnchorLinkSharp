using Newtonsoft.Json;

namespace EosioSigningRequest
{
    public class RequestSignature
    {
        [JsonProperty("signer")]
        //! Signer of this Signature
        public string Signer { get; set; }
        [JsonProperty("signature")]
        // The Signature of this Request
        public string Signature { get; set; }
    }
}