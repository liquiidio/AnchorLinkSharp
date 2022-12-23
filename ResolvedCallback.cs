namespace EosioSigningRequest
{
    /**
     * Context used to resolve a callback.
     * Compatible with the JSON response from a `push_transaction` call.
     */
    public class ResolvedCallback
    {
        /** The URL to hit. */
        public string Url;

        /**
         * Whether to run the request in the background. For a https url this
         * means POST in the background instead of a GET redirect.
         */
        public bool Background;

        /**
         * The callback payload as a object that should be encoded to JSON
         * and POSTed to background callbacks.
         */
        public CallbackPayload Payload;
    }
}