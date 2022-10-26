using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    /**
     * AnchorLink session that sends every request over the transport.
     * @internal
     */
    public class LinkFallbackSession : LinkSession, ILinkTransport
    {
        public override AnchorLink AnchorLink { get; set; }
        public override string Identifier { get; set; }
        public override string PublicKey { get; set; }
        public override PermissionLevel Auth { get; set; }
        public override Dictionary<string, object> Metadata { get; set; }
        public ILinkStorage Storage => throw new NotImplementedException();

        private readonly LinkFallbackSessionData _data;

        public LinkFallbackSession(AnchorLink anchorLink, LinkFallbackSessionData data, Dictionary<string,object> metadata /*, metadata: any*/) : base()
        {
            AnchorLink = anchorLink;
            Auth = data.Auth;
            PublicKey = data.PublicKey;
            Metadata = metadata ?? new Dictionary<string, object>();
            Identifier = data.Identifier;
            _data = data;
        }

        public void OnSuccess(SigningRequest request, TransactResult result)
        {
            AnchorLink.Transport.OnSuccess(request, result);
        }

        public void OnFailure(SigningRequest request, Exception exception)
        {
            AnchorLink.Transport.OnFailure(request, exception);
        }

        public void OnRequest(SigningRequest request, Action<object> cancel)
        {
            AnchorLink.Transport.OnSessionRequest(this, request, cancel);
            AnchorLink.Transport.OnRequest(request, cancel);
        }

        public Task<SigningRequest> Prepare(SigningRequest request, LinkSession session = null)
        {
            return AnchorLink.Transport.Prepare(request, this);
            // TODO hm   return Promise.resolve(request);
        }

        public void ShowLoading()
        {
            AnchorLink.Transport.ShowLoading();
        }

        public override LinkSignatureProvider MakeSignatureProvider()
        {
            return AnchorLink.MakeSignatureProvider(new []{PublicKey }, this);
        }

        public override async Task<TransactResult> Transact(TransactArgs args, TransactOptions options)
        {
            return await AnchorLink.Transact(args, options, this);
        }

        public override SerializedLinkSession Serialize()
        {
            return new SerializedLinkSession()
            {
                Data = _data,
                Metadata = Metadata,
            };
        }

        public void OnSessionRequest(LinkSession session, SigningRequest request, object cancel)
        {
            throw new NotImplementedException();
        }
    }
}