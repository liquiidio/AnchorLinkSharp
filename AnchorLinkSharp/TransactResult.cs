using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    /**
     * The result of a [[AnchorLink.transact]] call.
     */
    public class TransactResult
    {
        /** The signing request that was sent. */
        public SigningRequest Request;

        /** The transaction signatures. */
        public string[] Signatures { get; set; }

        /** The callback payload. */
        public CallbackPayload Payload;

        /** The signer authority. */
        public PermissionLevel Signer;

        /** The resulting transaction. */
        public Transaction Transaction;

        /** Serialized version of transaction. */
        public byte[] SerializedTransaction { get; set; }

        /** Push transaction response from api node, only present if transaction was broadcast. */
        public object Processed;//: {[key: string]: any}  // TODO
    }
}