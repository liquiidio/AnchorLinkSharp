namespace AnchorLinkSharp
{
    /**
     * Error that is thrown if an identity request fails to verify.
     * @internal
     */
    internal class IdentityException : LinkException
    {
        public new LinkErrorCode Code = LinkErrorCode.EIdentity;

        public IdentityException(string reason) : base($"Unable to verify identity {(reason != null ? "(" + reason + ")" : "")}") 
        { }
    }
}