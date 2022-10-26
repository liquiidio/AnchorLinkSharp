using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnchorLinkSharp
{
    /**
     * Type describing a anchorLink session that can create a eosjs compatible
     * signature provider and transact for a specific auth.
     */
    public abstract class LinkSession
    {
        /** The underlying anchorLink instance used by the session. */
        public abstract AnchorLink AnchorLink { get; set; }

        /** App identifier that owns the session. */
        public abstract string Identifier { get; set; }

        /** The public key the session can sign for. */
        public abstract string PublicKey { get; set; }

        /** The EOSIO auth (a.k.a. permission level) the session can sign for. */
        public abstract EosSharp.Core.Api.v1.PermissionLevel Auth { get; set; }

        /** Arbitrary metadata that will be serialized with the session. */
        public abstract Dictionary<string, object> Metadata { get; set; }//: {[key: string]: any}

        /** Creates a eosjs compatible signature provider that can sign for the session public key. */
        public abstract LinkSignatureProvider MakeSignatureProvider();

        /**
         * Transact using this session. See [[AnchorLink.transact]].
         */
        public abstract Task<TransactResult> Transact(TransactArgs args, TransactOptions options = null);

        /** Returns a JSON-encodable object that can be used recreate the session. */
        public abstract SerializedLinkSession Serialize();

        /**
         * Convenience, remove this session from associated [[AnchorLink]] storage if set.
         * Equivalent to:
         * ```ts
         * session.anchorLink.removeSession(session.identifier, session.auth)
         * ```
         */
        public async Task Remove()
        {
            if (AnchorLink.Storage != null)
            {
                await AnchorLink.RemoveSession(Identifier, Auth);
            }
        }

        /** Restore a previously serialized session. */
        public static LinkSession Restore(AnchorLink anchorLink, SerializedLinkSession data)
        {
            if(data.Data is LinkChannelSessionData channelSessionData)
                return new LinkChannelSession(anchorLink, channelSessionData, data.Metadata);
            else if(data.Data is LinkFallbackSessionData fallbackSessionData)
                    return new LinkFallbackSession(anchorLink, fallbackSessionData, data.Metadata);
            else
                throw new Exception("Unable to restore, session data invalid");
        }
    }
}
