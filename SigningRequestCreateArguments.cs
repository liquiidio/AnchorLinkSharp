using EosSharp;
using Action = EosSharp.Core.Api.v1.Action;

namespace EosioSigningRequest
{
    public class SigningRequestCreateArguments
    {

        /** Single action to create request with. */
        public Action Action;
        //public Action action;

        /** Multiple actions to create request with. */
        public Action[] Actions;

        /**
         * Full or partial transaction to create request with.
         * If TAPoS info is omitted it will be filled in when resolving the request.
         */
        public Transaction Transaction;

        /** Create an identity request. */
        public IdentityV2 Identity;

        /** Chain to use, defaults to EOS main-net if omitted. */
        public string ChainId;

        /** Whether wallet should broadcast tx, defaults to true. */
        public bool? Broadcast;

        /**
        * Optional callback URL the signer should hit after
        * broadcasting or signing. Passing a string means background = false.
        */
        public object Callback;

        /** Optional metadata to pass along with the request. */
        public object Info; // {[key: string]: string | Uint8Array}
        // Dictionary or string ?
    }
}