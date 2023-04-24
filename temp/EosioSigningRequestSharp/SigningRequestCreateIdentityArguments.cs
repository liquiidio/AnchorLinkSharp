namespace EosioSigningRequest
{
    public class SigningRequestCreateIdentityArguments
    {
        /**
         * Callback where the identity should be delivered.
         */
        public object Callback;

        /** Chain to use, defaults to EOS if omitted. */
        public string ChainId;

        /**
         * Requested account name of identity.
         * Defaults to placeholder (any identity) if omitted.
         */
        public string Account;

        /**
         * Requested account permission.
         * Defaults to placeholder (any permission) if omitted.
         */
        public string Permission;

        /** Optional metadata to pass along with the request. */
        public object Info; // {[key: string]: string | Uint8Array}
    }
}