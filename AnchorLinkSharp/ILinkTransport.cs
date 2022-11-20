using System;
using System.Threading.Tasks;
using EosioSigningRequest;

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
        void OnRequest(SigningRequest request, Action<object> cancel /*, cancel: (reason: string | Error) => void*/);

        /** Called if the request was successful. */
        void OnSuccess(SigningRequest request, TransactResult result);

        /** Called if the request failed. */
        void OnFailure(SigningRequest request, Exception exception);

        /**
         * Called when a session request is initiated.
         * @param session Session where the request originated.
         * @param request Signing request that will be sent over the session.
         */
        void OnSessionRequest(LinkSession session, SigningRequest request, Action<object> cancel);
        
        /*cancel: (reason: string | Error) => void)*/
  
        /** Can be implemented if transport provides a storage as well. */
        ILinkStorage Storage { get; }

        /** Can be implemented to modify request just after it has been created. */
        Task<SigningRequest> Prepare(SigningRequest request, LinkSession session = null);

        /** Called immediately when the transaction starts */
        void ShowLoading();
    }
}
