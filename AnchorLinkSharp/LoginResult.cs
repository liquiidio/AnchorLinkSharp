namespace AnchorLinkSharp
{
    /**
     * The result of a [[AnchorLink.login]] call.
     */
    public class LoginResult : IdentifyResult
    {
        /** The session created by the login. */
        public LinkSession Session { get; set; }
    }
}