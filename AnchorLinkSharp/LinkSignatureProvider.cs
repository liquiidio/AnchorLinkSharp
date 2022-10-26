using System.Threading.Tasks;
using EosioSigningRequest;

namespace AnchorLinkSharp
{
    public class LinkSignatureProvider
    {
        public AnchorLink AnchorLink;
        public ILinkTransport Transport;
        public string[] AvailableKeys;
        public SigningRequestEncodingOptions EncodingOptions;

        private async Task<string[]> GetAvailableKeys()
        {
            return AvailableKeys;
        }

        private async Task<TransactResult> Sign(LinkSignatureProviderArgs args)
        {
            SigningRequest request = SigningRequest.FromTransaction(
                args.ChainId,
                args.SerializedTransaction,
                EncodingOptions
            );

            request.SetCallback(AnchorLink.CreateCallbackUrl(), true);
            request.SetBroadcast(false);
            request = await Transport.Prepare(request);

            var result = await AnchorLink.SendRequest(request, Transport);

            return new TransactResult()
            {
                Request = request,
                SerializedTransaction = result.SerializedTransaction,
                Signatures = result.Signatures,
            };
        }
    }
}
