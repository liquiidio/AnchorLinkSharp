using System;

namespace AnchorLinkSharp
{
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
        public LinkErrorCode Code;

        protected LinkException(string reason) : base(reason)
        { }
    }
}