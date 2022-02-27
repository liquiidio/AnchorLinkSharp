using System.Collections.Generic;
using System.Threading.Tasks;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    public class LinkSignatureProvider
    {
        public Link link;
        public ILinkTransport transport;
        public string[] availableKeys;
        public SigningRequestEncodingOptions encodingOptions;

        private async Task<string[]> getAvailableKeys()
        {
            return availableKeys;
        }

        private async Task<TransactResult> Sign(LinkSignatureProviderArgs args)
        {
            SigningRequest request = SigningRequest.fromTransaction(
                args.chainId,
                args.serializedTransaction,
                encodingOptions
            );

            request.setCallback(link.createCallbackUrl(), true);
            request.setBroadcast(false);
            request = await transport.prepare(request);

            var result = await link.sendRequest(request, transport);

            return new TransactResult()
            {
                request = request,
                serializedTransaction = result.serializedTransaction,
                signatures = result.signatures,
            };
        }
    }

    public class LinkSignatureProviderArgs
    {
        // TODO
        public Dictionary<string, Abi> abis;
        public string chainId;
        public string[] requiredKeys;
        public byte[] serializedContextFreeData;
        public byte[] serializedTransaction;
    }
}
