using EosioSigningRequest;

namespace AnchorLinkSharp
{
    public class LinkOptions : ILinkOptions
    {
        public ILinkTransport Transport { get; set; }
        public string ChainId { get; set; }
        public object Rpc { get; set; }
        public string Service { get; set; } = "https://cb.anchor.link";
        public ILinkStorage Storage { get; set; }
        public IZlibProvider ZlibProvider { get; set; }
    }
}