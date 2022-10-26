namespace AnchorLinkSharp
{
    /**
     * Options for the [[AnchorLink.transact]] method.
     */
    public class TransactOptions
    {
        /**
         * Whether to broadcast the transaction or just return the signature.
         * Defaults to true.
         */
        public bool Broadcast { get; set; }
    }
}