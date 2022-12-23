using System;
using Newtonsoft.Json;

namespace EosioSigningRequest
{
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
}