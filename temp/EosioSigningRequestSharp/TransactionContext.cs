using System;

namespace EosioSigningRequest
{
    /**
     * Context used to resolve a transaction.
     * Compatible with the JSON response from a `get_block` call.
     */
    public class TransactionContext
    {
        /** Timestamp expiration will be derived from. */
        public DateTime? Timestamp;

        /**
         * How many seconds in the future to set expiration when deriving from timestamp.
         * Defaults to 60 seconds if unset.
         */
        public uint? ExpireSeconds;

        /** Block number ref_block_num will be derived from. */
        public ushort? BlockNum;

        /** Reference block number, takes precedence over block_num if both is set. */
        public ushort? RefBlockNum;

        /** Reference block prefix. */
        public uint? RefBlockPrefix;

        /** Expiration timestamp, takes precedence over timestamp and expire_seconds if set. */
        public DateTime? Expiration;
    }
}