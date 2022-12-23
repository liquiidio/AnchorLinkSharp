/** SigningRequest ABI and typedefs. */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EosioSigningRequest
{
    //! Static Class containing Constants used with the SigningRequest Package
    public static class AbiConstants
    {
        //! Constant to set the RequestFlags to None
        public const byte RequestFlagsNone = 0;
        //! Constant to set the RequestFlags to Broadcast
        public const byte RequestFlagsBroadcast = 1 << 0;
        //! Constant to set the RequestFlags to Background
        public const byte RequestFlagsBackground = 1 << 1;
    }

    public class TransactionHeader
    {
        [JsonProperty("expiration")]
        //!The time at which a transaction expires.
        public DateTime? Expiration { get; set; } /*time_point_sec*/

        [JsonProperty("ref_block_num")]
        //! Specifies a block num in the last 2^16 blocks.
        public ushort? RefBlockNum { get; set; } /*uint16*/

        [JsonProperty("ref_block_prefix")] 
        //! specifies the lower 32 bits of the blockid at get_ref_blocknum
        public uint? RefBlockPrefix { get; set; } /*uint32*/

        [JsonProperty("max_net_usage_words")]
        //! Upper limit on total network bandwidth (in 8 byte words) billed for this transaction.
        public uint? MaxNetUsageWords { get; set; } /*varuint32*/

        [JsonProperty("max_cpu_usage_words")] 
        //! Upper limit on the total CPU time billed for this transaction.
        public byte? MaxCpuUsageMs { get; set; } /*uint8*/

        [JsonProperty("delay_sec")] 
        //! Number of seconds to delay this transaction for during which it may be canceled.
        public uint? DelaySec { get; set; } /*varuint32*/
    }

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

    public class InfoPair
    {
        //! Default Constructor
        public InfoPair()
        {

        }

        //! Constructor with Key and Value mParameters
        public InfoPair(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }

        [JsonProperty("key")]
        //! Metadata Key
        public string Key { get; set; }

        [JsonProperty("value")]
        //! Metadata Value
        public object Value { get; set; } //: Uint8Array | string /*bytes*/
    }

    public class IdentityV2
    {
        [JsonProperty("permission")]
        //! Permission Requested
        public EosSharp.Core.Api.v1.PermissionLevel Permission { get; set; }
    }

    public class IdentityV3 : IdentityV2
    {
        [JsonProperty("scope")]
        //! Scope Requested
        public string Scope { get; set; }
    }

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