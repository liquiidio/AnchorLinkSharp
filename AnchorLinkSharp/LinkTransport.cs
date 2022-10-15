using System;
using System.Threading.Tasks;
using EosioSigningRequestSharp;

namespace AnchorLinkSharp
{
    /**
     * Protocol anchorLink transports need to implement.
     * A transport is responsible for getting the request to the
     * user, e.g. by opening request URIs or displaying QR codes.
     */
    public interface ILinkTransport
    {
        /**
     * Present a signing request to the user.
     * @param request The signing request.
     * @param cancel Can be called to abort the request.
     */
        void onRequest(SigningRequest request, System.Action<object> cancel /*, cancel: (reason: string | Error) => void*/);

        /** Called if the request was successful. */
        void onSuccess(SigningRequest request, TransactResult result);

        /** Called if the request failed. */
        void onFailure(SigningRequest request, Exception exception);

        /**
         * Called when a session request is initiated.
         * @param session Session where the request originated.
         * @param request Signing request that will be sent over the session.
         */
        void onSessionRequest(LinkSession session, SigningRequest request, object cancel);
        
        /*cancel: (reason: string | Error) => void)*/
  
        /** Can be implemented if transport provides a storage as well. */
        ILinkStorage storage { get; }

        /** Can be implemented to modify request just after it has been created. */
        Task<SigningRequest> prepare(SigningRequest request, LinkSession session = null);

        /** Called immediately when the transaction starts */
        void showLoading();
    }
}
