using System;

namespace AnchorLinkSharp
{
    public enum LinkErrorCode
    {
        E_DELIVERY,
        E_TIMEOUT,
        E_CANCEL,
        E_IDENTITY
    }

    /**
    * Error codes. Accessible using the `code` property on errors thrown by [[AnchorLink]] and [[LinkSession]].
    * - `E_DELIVERY`: Unable to request message to wallet.
    * - `E_TIMEOUT`: Request was delivered but user/wallet didn't respond in time.
    * - `E_CANCEL`: The [[LinkTransport]] canceled the request.
    * - `E_IDENTITY`: Identity proof failed to verify.
    */
    //type LinkErrorCode = 'E_DELIVERY' | 'E_TIMEOUT' | 'E_CANCEL' | 'E_IDENTITY'

    public class LinkException : Exception
    {
        public LinkErrorCode code;

        public LinkException(string reason) : base(reason)
        { }
    }

    /**
     * Error that is thrown if a [[LinkTransport]] cancels a request.
     * @internal
     */
    class CancelException : LinkException
    {
        public new LinkErrorCode code = LinkErrorCode.E_CANCEL;

        public CancelException(string reason) : base($"User canceled request {(reason != null ? "(" + reason + ")" : "") }")
        { }
    }

    /**
     * Error that is thrown if an identity request fails to verify.
     * @internal
     */
    class IdentityException : LinkException
    {
        public new LinkErrorCode code = LinkErrorCode.E_IDENTITY;

        public IdentityException(string reason) : base($"Unable to verify identity {(reason != null ? "(" + reason + ")" : "")}") 
        { }
    }

    /**
     * Error originating from a [[LinkSession]].
     * @internal
     */
    class SessionException : LinkException
    {
        public new LinkErrorCode code;// TODO //E_DELIVERY' | 'E_TIMEOUT'

        public SessionException(string reason, LinkErrorCode code /* 'E_DELIVERY' | 'E_TIMEOUT'*/) : base(reason)
        {
            this.code = code;
        }
    }
}