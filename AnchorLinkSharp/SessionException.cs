namespace AnchorLinkSharp
{
    /**
     * Error originating from a [[LinkSession]].
     * @internal
     */
    internal class SessionException : LinkException
    {
        public new LinkErrorCode Code;// TODO //E_DELIVERY' | 'E_TIMEOUT'

        public SessionException(string reason, LinkErrorCode code /* 'E_DELIVERY' | 'E_TIMEOUT'*/) : base(reason)
        {
            Code = code;
        }
    }
}