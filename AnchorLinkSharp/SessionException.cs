namespace AnchorLinkSharp
{
    /**
     * Error originating from a [[LinkSession]].
     * @internal
     */
    internal class SessionException : LinkException
    {
        public new LinkErrorCode Code;

        public SessionException(string reason, LinkErrorCode code) : base(reason)
        {
            Code = code;
        }
    }
}