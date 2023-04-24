using Newtonsoft.Json;

namespace EosioSigningRequest
{
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
}