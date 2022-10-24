using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using EosioSigningRequest;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Scripting;

namespace AnchorLinkSharp
{
    public abstract class LinkSessionDataBase
    {
        /** App identifier that owns the session. */
        public string identifier { get; set; }

        /** Authenticated user permission. */
        public PermissionLevel auth { get; set; }

        /** Public key of authenticated user */
        public string publicKey { get; set; }
    }

    public class Auth
    {
        public string actor;
        public string permission;
    }

    /**
     * Type describing a anchorLink session that can create a eosjs compatible
     * signature provider and transact for a specific auth.
     */
    public abstract class LinkSession
    {
        /** The underlying anchorLink instance used by the session. */
        public abstract AnchorLink AnchorLink { get; set; }

        /** App identifier that owns the session. */
        public abstract string identifier { get; set; }

        /** The public key the session can sign for. */
        public abstract string publicKey { get; set; }

        /** The EOSIO auth (a.k.a. permission level) the session can sign for. */
        public abstract EosSharp.Core.Api.v1.PermissionLevel auth { get; set; }

        /** Session type, e.g. 'channel'.  */
        public abstract string type { get; set; }

        /** Arbitrary metadata that will be serialized with the session. */
        public abstract Dictionary<string, object> metadata { get; set; }//: {[key: string]: any}

        /** Creates a eosjs compatible signature provider that can sign for the session public key. */
        public abstract LinkSignatureProvider makeSignatureProvider();

        /**
         * Transact using this session. See [[AnchorLink.transact]].
         */
        public abstract Task<TransactResult> transact(TransactArgs args, TransactOptions options = null);

        /** Returns a JSON-encodable object that can be used recreate the session. */
        public abstract SerializedLinkSession serialize();

        /**
         * Convenience, remove this session from associated [[AnchorLink]] storage if set.
         * Equivalent to:
         * ```ts
         * session.anchorLink.removeSession(session.identifier, session.auth)
         * ```
         */
        public async Task remove()
        {
            if (this.AnchorLink.storage != null)
            {
                await this.AnchorLink.removeSession(this.identifier, this.auth);
            }
        }

        /** Restore a previously serialized session. */
        public static LinkSession restore(AnchorLink anchorLink, SerializedLinkSession data)
        {
            if(data.data is LinkChannelSessionData channelSessionData)
                return new LinkChannelSession(anchorLink, channelSessionData, data.metadata);
            else if(data.data is LinkFallbackSessionData fallbackSessionData)
                    return new LinkFallbackSession(anchorLink, fallbackSessionData, data.metadata);
            else
                throw new Exception("Unable to restore, session data invalid");
        }
    }

    /** @internal */
    public class SerializedLinkSession
    {
        public string type { get ; set; }
        public Dictionary<string, object> metadata { get; set; }//: {[key: string]: any}

        [JsonConverter(typeof(LinkSessionDataConverter))]
        public LinkSessionDataBase data { get; set; } //data: any
    }

    public class LinkSessionDataConverter : JsonConverter<LinkSessionDataBase>
    {
        class SerializableLinkSessionWrapper
        {
            [JsonProperty("type")]
            public string Type;

            [JsonProperty("data")]
            public string Data;

#if Unity
            [Preserve]
#endif
            public SerializableLinkSessionWrapper()
            {

            }

            public SerializableLinkSessionWrapper(LinkSessionDataBase data)
            {
                Type = data.GetType().Name;
                this.Data = JsonConvert.SerializeObject(data);
            }
        }
        public override void WriteJson(JsonWriter writer, LinkSessionDataBase value, JsonSerializer serializer)
        {
            writer.WriteValue(JsonConvert.SerializeObject(new SerializableLinkSessionWrapper(value)));
        }

        public override LinkSessionDataBase ReadJson(JsonReader reader, Type objectType, LinkSessionDataBase existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var serializableLinkSessionWrapper = JsonConvert.DeserializeObject<SerializableLinkSessionWrapper>(reader.Value.ToString());
            if (serializableLinkSessionWrapper.Type == "LinkChannelSessionData")
                return JsonConvert.DeserializeObject<LinkChannelSessionData>(serializableLinkSessionWrapper.Data);
            if(serializableLinkSessionWrapper.Type == "LinkFallbackSessionData")
                return JsonConvert.DeserializeObject<LinkFallbackSessionData>(serializableLinkSessionWrapper.Data);
            throw new InvalidDataException($"Unknown SerializableLinkSessionWrapper.Data of type {serializableLinkSessionWrapper.Type}");
        }
    }

    /** @internal */
    public class ChannelInfo
    {
        /** Public key requests are encrypted to. */
        public string key { get; set; }

        /** The wallet given channel name, usually the device name. */
        public string name { get; set; }

        /** The channel push url. */
        public string url { get; set; }
    }

    /** @internal */
    public class LinkChannelSessionData : LinkSessionDataBase
    {
        /** The wallet channel url. */
        public ChannelInfo channel { get; set; }

        /** The private request key. */
        public string requestKey { get; set; }
    }

    /**
     * AnchorLink session that pushes requests over a channel.
     * @internal
     */
    public class LinkChannelSession : LinkSession, ILinkTransport
    {
        readonly Timer timeoutTimer = new Timer(); // Timer anlegen
        public override AnchorLink AnchorLink { get; set; }
        public override string identifier { get; set; }
        public override string publicKey { get; set; }
        public override PermissionLevel auth { get; set; }
        public override string type { get; set; }   // TODO remove here and from base
        public override Dictionary<string, object> metadata { get; set; }
        public ILinkStorage storage { get; }
        private ChannelInfo channel;
        private int timeout = 2 * 60 * 1000; // ms
        Func<SigningRequest, byte[]> encrypt;

        private LinkChannelSessionData _data;

        public LinkChannelSession(AnchorLink anchorLink, LinkChannelSessionData data , Dictionary<string, object> metadata) : base()
        {
            this.AnchorLink = anchorLink;
            this.auth = data.auth;
            this.publicKey = data.publicKey;
            this.channel = data.channel;
            this.identifier = data.identifier;
            this.encrypt = (request) =>
                LinkUtils.sealMessage(request.encode(true, false), data.requestKey, data.channel.key);
            this.metadata = metadata ?? new Dictionary<string, object>();
            this.metadata.Add("timeout", this.timeout);
            this.metadata.Add("name", this.channel.name);
            this._data = data;
        }

        public void onSuccess(SigningRequest request, TransactResult result)
        {
            this.AnchorLink.transport.onSuccess(request, result);
        }

        public void onFailure(SigningRequest request, Exception exception)
        {
            this.AnchorLink.transport.onFailure(request, exception);
        }

        public async void onRequest(SigningRequest request, Action<object> cancel)
        {
            LinkInfo info = new LinkInfo()
            {
                expiration = DateTime.Now.AddSeconds(this.timeout)
            };

            this.AnchorLink.transport.onSessionRequest(this, request, cancel);

            timeoutTimer.Interval = timeout + 500;    // in ms
            timeoutTimer.Elapsed += (source, e) =>
            {
                cancel(new SessionException("Wallet did not respond in time", LinkErrorCode.E_TIMEOUT));
            };
            timeoutTimer.Start(); // start Timer

            request.data.info.Add(new InfoPair() {key = "anchorLink", value = new object()}); /*value =abiEncode(info, "link_info") TODO */ //)};

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("X-Buoy-Wait", "10");
                    var response = await httpClient.PostAsync(this.channel.url, new ByteArrayContent(this.encrypt(request)));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        cancel(new SessionException("Unable to push message", LinkErrorCode.E_DELIVERY));
                    }
                }
            }
            catch (Exception ex)
            {
                cancel(new SessionException($"Unable to reach anchorLink service ({ex.Message ?? ex.ToString()})", LinkErrorCode.E_DELIVERY));

            }
        }

        public async Task<SigningRequest> prepare(SigningRequest request, LinkSession session = null)
        {
            return await this.AnchorLink.transport.prepare(request, this);
//            return Promise.resolve(request); TODO hm?
        }

        public void showLoading()
        {
            this.AnchorLink.transport.showLoading();
        }

        public override LinkSignatureProvider makeSignatureProvider()
        {
            return this.AnchorLink.makeSignatureProvider(new []{this.publicKey }, this);
        }

        public override async Task<TransactResult> transact(TransactArgs args, TransactOptions options = null) {
            return await this.AnchorLink.transact(args, options, this);
        }

        public override SerializedLinkSession serialize()
        {
            return new SerializedLinkSession()
            {
                type = "channel",
                data = _data,
                metadata = this.metadata
            };
        }

        public void onSessionRequest(LinkSession session, SigningRequest request, object cancel)
        {
            throw new NotImplementedException();
        }

    }

    /** @internal */
    public class LinkFallbackSessionData : LinkSessionDataBase
    {

    }

    /**
     * AnchorLink session that sends every request over the transport.
     * @internal
     */
    public class LinkFallbackSession : LinkSession, ILinkTransport
    {
        public override AnchorLink AnchorLink { get; set; }
        public override string identifier { get; set; }
        public override string publicKey { get; set; }
        public override PermissionLevel auth { get; set; }
        public override string type { get; set; }
        public override Dictionary<string, object> metadata { get; set; }
        public ILinkStorage storage => throw new NotImplementedException();

        private LinkFallbackSessionData _data;

        public LinkFallbackSession(AnchorLink anchorLink, LinkFallbackSessionData data, Dictionary<string,object> metadata /*, metadata: any*/) : base()
        {
            this.AnchorLink = anchorLink;
            this.auth = data.auth;
            this.publicKey = data.publicKey;
            this.metadata = metadata ?? new Dictionary<string, object>();
            this.identifier = data.identifier;
            this._data = data;
        }

        public void onSuccess(SigningRequest request, TransactResult result)
        {
            this.AnchorLink.transport.onSuccess(request, result);
        }

        public void onFailure(SigningRequest request, Exception exception)
        {
            this.AnchorLink.transport.onFailure(request, exception);
        }

        public void onRequest(SigningRequest request, Action<object> cancel)
        {
            this.AnchorLink.transport.onSessionRequest(this, request, cancel);
            this.AnchorLink.transport.onRequest(request, cancel);
        }

        public Task<SigningRequest> prepare(SigningRequest request, LinkSession session = null)
        {
            return this.AnchorLink.transport.prepare(request, this);
         // TODO hm   return Promise.resolve(request);
        }

        public void showLoading()
        {
            this.AnchorLink.transport.showLoading();
        }

        public override LinkSignatureProvider makeSignatureProvider()
        {
            return this.AnchorLink.makeSignatureProvider(new []{this.publicKey }, this);
        }

        public override async Task<TransactResult> transact(TransactArgs args, TransactOptions options)
        {
            return await this.AnchorLink.transact(args, options, this);
        }

        public override SerializedLinkSession serialize()
        {
            return new SerializedLinkSession()
            {
                type = this.type,
                data = _data,
                metadata = this.metadata,
            };
        }

        public void onSessionRequest(LinkSession session, SigningRequest request, object cancel)
        {
            throw new NotImplementedException();
        }
    }
}
