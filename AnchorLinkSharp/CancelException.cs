namespace AnchorLinkSharp
{
    /**
     * Error that is thrown if a [[LinkTransport]] cancels a request.
     * @internal
     */
    internal class CancelException : LinkException
    {
        public new LinkErrorCode Code = LinkErrorCode.ECancel;

        public CancelException(string reason) : base($"User canceled request {(reason != null ? "(" + reason + ")" : "") }")
        { }
    }
}