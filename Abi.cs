/** SigningRequest ABI and typedefs. */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EosioSigningRequest
{
    public static class AbiConstants
    {
        public const byte RequestFlagsNone = 0;
        public const byte RequestFlagsBroadcast = 1 << 0;
        public const byte RequestFlagsBackground = 1 << 1;
    }

    public class TransactionHeader
    {
        [JsonProperty("expiration")]
        public DateTime? Expiration { get; set; } /*time_point_sec*/
        [JsonProperty("ref_block_num")]
        public ushort? RefBlockNum { get; set; } /*uint16*/
        [JsonProperty("ref_block_prefix")] 
        public uint? RefBlockPrefix { get; set; } /*uint32*/
        [JsonProperty("max_net_usage_words")] 
        public uint? MaxNetUsageWords { get; set; } /*varuint32*/
        [JsonProperty("max_cpu_usage_words")] 
        public byte? MaxCpuUsageMs { get; set; } /*uint8*/
        [JsonProperty("delay_sec")] 
        public uint? DelaySec { get; set; } /*varuint32*/
    }

    public class SigningRequestData
    {
        [JsonProperty("chain_id")]
        public KeyValuePair<string, object> ChainId { get; set; }
        [JsonProperty("req")]
        public KeyValuePair<string, object> Req { get; set; }
        [JsonProperty("flags")]
        public byte Flags { get; set; }
        [JsonProperty("callback")]
        public string Callback { get; set; }
        [JsonProperty("info")]
        public List<object> Info { get; set; }
    }

    public class InfoPair
    {
        public InfoPair()
        {

        }

        public InfoPair(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }

        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("value")]
        public object Value { get; set; } //: Uint8Array | string /*bytes*/
    }

    public class IdentityV2
    {
        [JsonProperty("permission")]
        public EosSharp.Core.Api.v1.PermissionLevel Permission { get; set; }
    }

    public class IdentityV3 : IdentityV2
    {
        [JsonProperty("scope")]
        public string Scope { get; set; }
    }

    public class RequestSignature
    {
        [JsonProperty("signer")]
        public string Signer { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}