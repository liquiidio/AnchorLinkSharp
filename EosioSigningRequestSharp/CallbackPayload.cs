using System.Collections.Generic;

namespace EosioSigningRequest
{
    /**
     * The callback payload sent to background callbacks.
     */
    public class CallbackPayload
    {
        /** The first signature. */
        public string Sig;

        /** Transaction ID as HEX-encoded string. */
        public string Tx;

        /** Block number hint (only present if transaction was broadcast). */
        public string Bn;

        /** Signer authority, aka account name. */
        public string Sa;

        /** Signer permission, e.g. "active". */
        public string Sp;

        /** Reference block num used when resolving request. */
        public string Rbn;

        /** Reference block id used when resolving request. */
        public string Rid;

        /** The originating signing request packed as a uri string. */
        public string Req;

        /** Expiration time used when resolving request. */
        public string Ex;

        /** All signatures 0-indexed as `sig0`, `sig1`, etc. */
        //public Dictionary<string, string> Sigs; // TODO

        //    [sig0: string]: string | undefined
        public Dictionary<string, string> Data;
    }
}