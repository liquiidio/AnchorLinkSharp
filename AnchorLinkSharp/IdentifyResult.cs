namespace AnchorLinkSharp
{
    /**
     * The result of a [[AnchorLink.identify]] call.
     */
    public class IdentifyResult : TransactResult {
        /** The identified account. */
        public object Account { get; set; }

        /** The public key that signed the identity proof.  */
        public string SignerKey { get; set; }
    }
}