using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;

namespace AnchorLinkSharp
{
    /**
     * AnchorLink session that pushes requests over a channel.
     * @internal
     */
    public class LinkChannelSession : LinkSession, ILinkTransport
    {
        private readonly Timer _timeoutTimer = new Timer();
        public override AnchorLink AnchorLink { get; set; }
        public override string Identifier { get; set; }
        public override string PublicKey { get; set; }
        public override PermissionLevel Auth { get; set; }
        public override Dictionary<string, object> Metadata { get; set; }
        public ILinkStorage Storage { get; }

        private readonly ChannelInfo _channel;
        
        private readonly int _timeout = 2 * 60 * 1000; // ms
        
        private readonly Func<SigningRequest, byte[]> _encrypt;

        private readonly LinkChannelSessionData _data;

        public LinkChannelSession(AnchorLink anchorLink, LinkChannelSessionData data , Dictionary<string, object> metadata) : base()
        {
            AnchorLink = anchorLink;
            Auth = data.Auth;
            PublicKey = data.PublicKey;
            _channel = data.Channel;
            Identifier = data.Identifier;
            _encrypt = (request) =>
                LinkUtils.SealMessage(request.Encode(true, false), data.RequestKey, data.Channel.Key);
            Metadata = metadata ?? new Dictionary<string, object>();
            Metadata.Add("timeout", _timeout);
            Metadata.Add("name", _channel.Name);
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

        public async void OnRequest(SigningRequest request, Action<object> cancel)
        {
            var info = new LinkInfo()
            {
                Expiration = DateTime.Now.AddSeconds(_timeout)
            };

            AnchorLink.Transport.OnSessionRequest(this, request, cancel);

            _timeoutTimer.Interval = _timeout + 500;    // in ms
            _timeoutTimer.Elapsed += (source, e) =>
            {
                cancel(new SessionException("Wallet did not respond in time", LinkErrorCode.ETimeout));
            };
            _timeoutTimer.Start(); // start Timer

            request.Data.Info.Add(new InfoPair() {Key = "anchorLink", Value = AnchorLink.SerializeStructData(info,
                    LinkAbiData.Types.structs.FirstOrDefault(lad => lad.name == "link_info"), LinkAbiData.Types)});
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("X-Buoy-Wait", "10");
                    var response = await httpClient.PostAsync(_channel.Url, new ByteArrayContent(_encrypt(request)));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        cancel(new SessionException("Unable to push message", LinkErrorCode.EDelivery));
                    }
                }
            }
            catch (Exception ex)
            {
                cancel(new SessionException($"Unable to reach anchorLink service ({ex.Message})", LinkErrorCode.EDelivery));

            }
        }

        public async Task<SigningRequest> Prepare(SigningRequest request, LinkSession session = null)
        {
            return await AnchorLink.Transport.Prepare(request, this);
//            return Promise.resolve(request); TODO hm?
        }

        public void ShowLoading()
        {
            AnchorLink.Transport.ShowLoading();
        }

        public override LinkSignatureProvider MakeSignatureProvider()
        {
            return AnchorLink.MakeSignatureProvider(new []{PublicKey }, this);
        }

        public override async Task<TransactResult> Transact(TransactArgs args, TransactOptions options = null) {
            return await AnchorLink.Transact(args, options, this);
        }

        public override SerializedLinkSession Serialize()
        {
            return new SerializedLinkSession()
            {
                Type = "channel",
                Data = _data,
                Metadata = Metadata
            };
        }

        public void OnSessionRequest(LinkSession session, SigningRequest request, Action<object> cancel)
        {
            throw new NotImplementedException();
        }

    }
}