namespace AnchorLinkSharp
{
    /** @internal */
    public class ChannelInfo
    {
        /** Public key requests are encrypted to. */
        public string Key { get; set; }

        /** The wallet given channel name, usually the device name. */
        public string Name { get; set; }

        /** The channel push url. */
        public string Url { get; set; }
    }
}