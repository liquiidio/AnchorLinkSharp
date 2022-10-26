namespace AnchorLinkSharp
{
    /** @internal */
    public class LinkChannelSessionData : LinkSessionDataBase
    {
        /** The wallet channel url. */
        public ChannelInfo Channel { get; set; }

        /** The private request key. */
        public string RequestKey { get; set; }
    }
}