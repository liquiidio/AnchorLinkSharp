namespace AnchorLinkSharp
{
    /**
     * Payload accepted by the [[AnchorLink.transact]] method.
     * Note that one of `action`, `actions` or `transaction` must be set.
     */
    public class TransactArgs
    {
        /** Full transaction to sign. */
        public EosSharp.Core.Api.v1.Transaction Transaction;

        /** Action to sign. */
        public EosSharp.Core.Api.v1.Action Action;

        /** Actions to sign. */
        public EosSharp.Core.Api.v1.Action[] Actions;
    }
}