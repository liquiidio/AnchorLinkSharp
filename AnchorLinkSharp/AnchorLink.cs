using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using EosioSigningRequest;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using EosSharp.Core.Providers;
using EosSharp;
using EosSharp.Core.Interfaces;
using Newtonsoft.Json;

namespace AnchorLinkSharp
{
    public static class LinkConstants
    {
        /**
         * Format a EOSIO permission level in the format `actor@permission` taking placeholders into consideration.
         * @internal
         */
        public static string formatAuth(PermissionLevel auth)
        {
            var actor = auth.actor;
            var permission = auth.permission;

            if (actor == Constants.PlaceholderName)
            {
                actor = "<any>";
            }
            if (permission == Constants.PlaceholderName || permission == Constants.PlaceholderPermission)
            {
                permission = "<any>";
            }

            return $"{actor}@{permission}";
        }
    }

    /**
     * Payload accepted by the [[AnchorLink.transact]] method.
     * Note that one of `action`, `actions` or `transaction` must be set.
     */
    public class TransactArgs
    {
        /** Full transaction to sign. */
        public EosSharp.Core.Api.v1.Transaction transaction;

        /** Action to sign. */
        public EosSharp.Core.Api.v1.Action action;

        /** Actions to sign. */
        public EosSharp.Core.Api.v1.Action[] actions;
    }

    /**
     * Options for the [[AnchorLink.transact]] method.
     */
    public class TransactOptions
    {
        /**
         * Whether to broadcast the transaction or just return the signature.
         * Defaults to true.
         */
        public bool broadcast { get; set; }
    }

    /**
     * The result of a [[AnchorLink.transact]] call.
     */
    public class TransactResult
    {
        /** The signing request that was sent. */
        public SigningRequest request;

        /** The transaction signatures. */
        public string[] signatures { get; set; }

        /** The callback payload. */
        public CallbackPayload payload;

        /** The signer authority. */
        public PermissionLevel signer;

        /** The resulting transaction. */
        public EosSharp.Core.Api.v1.Transaction transaction;

        /** Serialized version of transaction. */
        public byte[] serializedTransaction { get; set; }

        /** Push transaction response from api node, only present if transaction was broadcast. */
        public object processed;//: {[key: string]: any}  // TODO
    }

    /**
     * The result of a [[AnchorLink.identify]] call.
     */
    public class IdentifyResult : TransactResult {
        /** The identified account. */
        public object account { get; set; }

        /** The public key that signed the identity proof.  */
        public string signerKey { get; set; }
    }

    /**
     * The result of a [[AnchorLink.login]] call.
     */
    public class LoginResult : IdentifyResult
    {
        /** The session created by the login. */
        public LinkSession session { get; set; }
    }

    /**
     * Main class, also exposed as the default export of the library.
     *
     * Example:
     *
     * ```ts
     * import AnchorLink from 'anchor-anchorLink'
     * import ConsoleTransport from 'anchor-anchorLink-console-transport'
     *
     * const anchorLink = new AnchorLink({
     *     transport: new ConsoleTransport()
     * })
     *
     * const result = await anchorLink.transact({actions: myActions})
     * ```
     */
    public class AnchorLink : AbiSerializationProvider
    {
        /** The eosjs RPC instance used to communicate with the EOSIO node. */
        //public readonly EosApi rpc;

        /** Transport used to deliver requests to the user wallet. */
        public readonly ILinkTransport transport;

        /** EOSIO ChainID for which requests are valid. */
        public readonly string chainId;

        /** PlayerPrefsStorage adapter used to persist sessions. */
        public readonly ILinkStorage storage;

        private string serviceAddress;
        private SigningRequestEncodingOptions requestOptions;

        private Dictionary<string, Abi> abiCache = new Dictionary<string, Abi>();
        private Dictionary<string, Task<GetAbiResponse>> pendingAbis = new Dictionary<string, Task<GetAbiResponse>>();

        /** Create a new anchorLink instance. */
        public AnchorLink(ILinkOptions options)
        {
            
            if (options.transport == null)
            {
                throw new Exception("options.transport is required, see https://github.com/greymass/anchor-anchorLink#transports");
            }

            if (options.ZlibProvider == null)
            {
                throw new Exception("options.ZlibProvider is required");
            }

            if (options.chainId != null)
            {
                this.chainId = options.chainId is long chainIdLong
                    ? Constants.nameToId(chainIdLong)
                    : (string) options.chainId;
            }
            else
            {
                this.chainId = Defaults.chainId;
            }

            if (options.rpc is string && !string.IsNullOrEmpty((string) options.rpc))
            {
                this.Api = new EosApi(new EosConfigurator()
                {
                    ChainId = this.chainId,
                    ExpireSeconds = 10,
                    HttpEndpoint = (string) options.rpc,
                }, new HttpHandler());
            }
            else if(options.rpc is EosApi eosApi)
            {
                this.Api = eosApi;
            }

            this.serviceAddress = (options.service ?? Defaults.service).Trim(); //.replace(/\/$/, '') TODO
            this.transport = options.transport;
            if (options.storage != null)
            {
                this.storage = options.storage ?? this.transport.storage;
            }

            this.requestOptions = new SigningRequestEncodingOptions()
            {
                abiSerializationProvider = this,
                signatureProvider = new DefaultSignProvider(),
                zlib = options.ZlibProvider
            };
        }


        /**
         * Fetch the ABI for given account, cached.
         * @internal
         */
        public async Task<Abi> GetAbi(string account)
        {
            var rv = this.abiCache[account];
            if (rv == null)
            {
                var getAbi = this.pendingAbis[account];
                if (getAbi == null)
                {
                    getAbi = this.Api.GetAbi(new GetAbiRequest() {account_name = account});
                    this.pendingAbis.Add(account, getAbi);
                }

                rv = (await getAbi).abi;
                this.pendingAbis.Remove(account);
                if (rv != null)
                {
                    this.abiCache.Add(account, rv);
                }
            }

            return rv;
        }

        /**
         * Create a new unique buoy callback url.
         * @internal
         */
        public string createCallbackUrl()
        {
            return $"{this.serviceAddress}/{Guid.NewGuid()}";
        }

        /**
         * Create a SigningRequest instance configured for this anchorLink.
         * @internal
         */
        public async Task<SigningRequest> createRequest(SigningRequestCreateArguments args, ILinkTransport transport = null)
        {
            var t = transport ?? this.transport;
            // generate unique callback url
            var request = await SigningRequest.create(
                new SigningRequestCreateArguments()
                {
                    action = args.action,
                    transaction = args.transaction,
                    chainId = this.chainId,
                    broadcast = false,
                    callback = new Dictionary<string, object>()
                    {
                        {"url", this.createCallbackUrl()},
                        {"background", true},
                    },
                    Identity = args.Identity
                },
                this.requestOptions
            );
            request = await t.prepare(request);

            return request;
        }

        /**
         * Send a SigningRequest instance using this anchorLink.
         * @internal
         */
        public async Task<TransactResult> sendRequest(SigningRequest request, ILinkTransport transport,
            bool broadcast = false)
        {
            var t = transport ?? this.transport;
            try
            {
                var linkUrl = request.data.callback;
                if (!linkUrl.StartsWith(this.serviceAddress))
                {
                    throw new Exception("Request must have a anchor.link callback");
                }

                if (request.data.flags != 2)
                {
                    throw new Exception("Invalid request flags");
                }

                // wait for callback or user cancel
                var cts = new CancellationTokenSource();
                var socket = waitForCallback(linkUrl, cts);
                //    .ContinueWith((data) =>
                //{
                //    if (data.IsCanceled)
                //    {
                //        throw new CancelException($"Task cancelled");
                //    }

                //    if (data.Exception != null)
                //    {
                //        throw new CancelException($"Rejected by wallet: {data.Exception.Message}");
                //    }

                //    return data;
                //}, cts.Token);
                var token = cts.Token;

                //var cancel = Task.Run(() =>
                //{
                t.onRequest(request, (reason) =>
                {
                    if(!cts.IsCancellationRequested)
                        cts.Cancel();

                    if (reason is string sreason)
                    {
                        // TODO, hm
                        cts.Cancel();
                        throw new CancelException(sreason);
                    }
                });
                //}, token);

                //var poll = pollForCallback(linkUrl, token);
                await socket;
                CallbackPayload payload = socket.Result;
                PermissionLevel signer = new PermissionLevel()
                {
                    actor = payload.sa,
                    permission = payload.sp,
                };

                List<string> signatures = new List<string>(){ payload.sig }; // TODO, multiple sigs?

                // recreate transaction from request response
                var resolved = await ResolvedSigningRequest.fromPayload(
                    payload,
                    this.requestOptions,
                    this
                );
                var info = resolved.request.getInfos();
                if (info.ContainsKey("fuel_sig"))
                {
                    signatures.Insert(0, info["fuel_sig"]);
                }

                var transaction = resolved.transaction;
                var serializedTransaction = resolved.serializedTransaction;
                TransactResult result = new TransactResult()
                {
                    request = resolved.request,
                    serializedTransaction = serializedTransaction,
                    transaction = transaction,
                    signatures = signatures.ToArray(),
                    payload = payload,
                    signer = signer,
                };
                if (broadcast)
                {
                    var res = await this.Api.PushTransaction(new PushTransactionRequest()
                    {
                        signatures = result.signatures,
                        transaction = transaction,
                        compression = 0, // TODO ?
                        // TODO ! pass other properties
                    });
                    result.processed = res.processed;
                }

                t.onSuccess(request, result);

                return result;
            }
            catch (Exception ex)
            {
                t.onFailure(request, ex);
                throw ex;
            }
        }

        /**
         * Sign and optionally broadcast a EOSIO transaction, action or actions.
         *
         * Example:
         *
         * ```ts
         * var result = await myLink.transact({transaction: myTx})
         * ```
         *
         * @param args The action, actions or transaction to use.
         * @param options Options for this transact call.
         * @param transport Transport override, for internal use.
         */
        public async Task<TransactResult> transact(TransactArgs args, TransactOptions options = null, ILinkTransport transport = null)
        {
            var t = transport ?? this.transport;
            var broadcast = options == null || options.broadcast;
            // Initialize the loading state of the transport
            t.showLoading();

            // eosjs transact compat: upgrade to transaction if args have any header field
            // TODO
            //if (args.action != null || args.actions != null)
            //{
            //    args.transaction = new EosSharp.Core.Api.v1.Transaction()
            //    {
            //        expiration = new DateTime(1970, 1, 1),
            //        ref_block_num = 0,
            //        ref_block_prefix = 0,
            //        max_net_usage_words = 0,
            //        max_cpu_usage_ms = 0,
            //        delay_sec = 0
            //    };
            //}

            var signingRequestCreateArguments = new SigningRequestCreateArguments()
            {
                transaction = args.transaction,
                action = args.action,
                actions = args.actions,
                chainId = this.chainId,
                broadcast = broadcast,
                callback = this.createCallbackUrl()
            };

            var request = await this.createRequest(signingRequestCreateArguments, t);
            var result = await this.sendRequest(request, t, broadcast);
            return result;
        }

        /**
         * Send an identity request and verify the identity proof.
         * @param requestPermission Optional request permission if the request is for a specific account or permission.
         * @param info Metadata to add to the request.
         * @note This is for advanced use-cases, you probably want to use [[AnchorLink.login]] instead.
         */
        public async Task<IdentifyResult> identify(/*TODO Scope */PermissionLevel requestPermission, object info /*, info?: {[key: string]: string | Uint8Array}*/)
        {
            var request = await this.createRequest(new SigningRequestCreateArguments()
            {
                Identity = new IdentityV2()
                {
                    permission = requestPermission,
                },
                info = info// (List<InfoPair>)info,
            });

            /*string test = "";
            pollForCallback(request.data.callback, CancellationToken.None);*/
            //pollForCallback(request.data.callback, CancellationToken.None);
            var res = await this.sendRequest(request, null); // TODO
            if (!res.request.isIdentity())
            {
                throw new IdentityException("Unexpected response");
            }

            //var memStream = new MemoryStream();

            //var chainIdBuff = SerializationHelper.HexStringToByteArray(request.getChainId());
            //memStream.Write(chainIdBuff, 0, chainIdBuff.Length);
            //memStream.Write(res.serializedTransaction, 0, res.serializedTransaction.Length);
            //memStream.Write(new byte[32], 0, 32);
            //var message = memStream.ToArray();  // TODO

            var signer = res.signer;
            //string signerKey = ""; //  ecc.recover(res.signatures[0], message)   // TODO
            var account = await this.Api.GetAccount(new GetAccountRequest() {account_name = signer.actor});
            if (account == null)
            {
                throw new IdentityException($"Signature from unknown account: {signer.actor}");
            }

            var permission = account.permissions.SingleOrDefault(p => p.perm_name == signer.permission);
            if (permission == null)
            {
                throw new IdentityException($"{signer.actor} signed for unknown permission: {signer.permission}");
            }

            //var auth = permission.required_auth;
            //var keyAuth = auth.keys.SingleOrDefault(key => key.key == signerKey); // TODO key-equal-func
            //if (keyAuth == null)
            //{
            //    throw new IdentityException($"{LinkConstants.formatAuth(signer)} has no key matching id signature");
            //}

            //if (auth.threshold > keyAuth.weight)
            //{
            //    throw new IdentityException(
            //        $"{LinkConstants.formatAuth(signer)} signature does not reach auth threshold");
            //}

            if (requestPermission != null)
            {
                if ((requestPermission.actor != Constants.PlaceholderName && requestPermission.actor != signer.actor) ||
                    (requestPermission.permission != Constants.PlaceholderPermission &&
                     requestPermission.permission != signer.permission)
                )
                {
                    throw new IdentityException(
                        $"Unexpected identity proof from {LinkConstants.formatAuth(signer)}, expected {LinkConstants.formatAuth(requestPermission)}");
                }
            }

            return new IdentifyResult()
            {
                payload = res.payload,
                signatures = res.signatures,
                processed = res.processed,
                signer = res.signer,
                request = res.request,
                transaction = res.transaction,
                serializedTransaction = res.serializedTransaction,
                account = account,
                //signerKey = signerKey
            };
        }

        /**
         * Login and create a persistent session.
         * @param identifier The session identifier, an EOSIO name (`[a-z1-5]{1,12}`).
         *                   Should be set to the contract account if applicable.
         */
        public async Task<LoginResult> login(string identifier)
        {
            var keyPair = CryptoHelper.GenerateKeyPair();
            var privateKey = keyPair.PrivateKey;
            var requestKey = keyPair.PublicKey;
            var createInfo = new LinkCreate()
            {
                session_name = identifier,
                request_key = requestKey,
            };
            this.requestOptions.signatureProvider = new DefaultSignProvider(privateKey);

            var res = await this.identify(Constants.PlaceholderAuth, LinkUtils.abiEncode(createInfo, "link_create"));
            var metadata = new Dictionary<string, object>();
            var rawInfo = res.request.getRawInfo();
            if(rawInfo.ContainsKey("return_path"))
                metadata.Add("sameDevice", rawInfo["return_path"]);
            LinkSession session;
            if (res.payload.data.ContainsKey("link_ch") && res.payload.data.ContainsKey("link_key") &&
                res.payload.data.ContainsKey("link_name"))
            {
                session = new LinkChannelSession(
                    this, new LinkChannelSessionData()
                    {
                        identifier = identifier,
                        auth = res.signer,
                        publicKey = res.signerKey,
                        channel = new ChannelInfo()
                        {
                            url = res.payload.data["link_ch"],
                            key = res.payload.data["link_key"],
                            name = res.payload.data["link_name"]
                        },
                        requestKey = privateKey
                    },
                    metadata
                );
            }
            else
            {
                session = new LinkFallbackSession(
                    this, new LinkFallbackSessionData()
                    {
                        identifier = identifier,
                        auth = new PermissionLevel()
                        {
                            actor = res.signer.actor,
                            permission = res.signer.permission
                        }
                    },
                    metadata
                );
            }

            if (this.storage != null)
            {
                await this.storeSession(identifier, session);
            }

            return new LoginResult
            {
                payload = res.payload,
                transaction = res.transaction,
                signatures = res.signatures,
                signerKey = res.signerKey,
                account = res.account,
                serializedTransaction = res.serializedTransaction,
                signer = res.signer,
                processed = res.processed,
                request = res.request,
                session = session
            };
        }

        /**
         * Restore previous session, see [[AnchorLink.login]] to create a new session.
         * @param identifier The session identifier, should be same as what was used when creating the session with [[AnchorLink.login]].
         * @param auth A specific session auth to restore, if omitted the most recently used session will be restored.
         * @returns A [[LinkSession]] instance or null if no session can be found.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error retrieving the session data.
         **/
        public async Task<LinkSession> restoreSession(string identifier, PermissionLevel auth = null)
        {
            if (this.storage == null)
            {
                throw new Exception("Unable to restore session: No storage adapter configured");
            }

            string key = "";
            if (auth != null)
            {
                key = this.sessionKey(identifier, LinkConstants.formatAuth(auth));
            }
            else
            {
                var latestPermissions = (await this.listSessions(identifier));
                if (latestPermissions.Count > 0)
                {
                    var latest = latestPermissions[0];
                    if (latest == null)
                    {
                        return null;
                    }

                    key = this.sessionKey(identifier, LinkConstants.formatAuth(latest));
                }
            }

            var data = await this.storage.read(key);
            if (data == null)
            {
                return null;
            }

            SerializedLinkSession sessionData = null;
            try
            {
                sessionData = JsonConvert.DeserializeObject<SerializedLinkSession>(data);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to restore session: Stored JSON invalid ({ex.Message ?? ex.ToString()})");
            }

            var session = LinkSession.restore(this, sessionData);
            if (auth != null)
            {
                // update latest used
                await this.touchSession(identifier, auth);
            }

            return session;
        }

        /**
         * List stored session auths for given identifier.
         * The most recently used session is at the top (index 0).
         * @throws If no [[LinkStorage]] adapter is configured or there was an error retrieving the session list.
         **/
        public async Task<List<PermissionLevel>> listSessions(string identifier)
        {
            if (this.storage == null)
            {
                throw new Exception("Unable to list sessions: No storage adapter configured");
            }

            var key = this.sessionKey(identifier, "list");
            List<PermissionLevel> list;
            try
            {
                list = JsonConvert.DeserializeObject<List<PermissionLevel>>((await this.storage.read(key)) ?? "{}") ?? new List<PermissionLevel>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to list sessions: Stored JSON invalid ({ex.Message ?? ex.ToString()})");
            }

            return list;
        }

        /**
         * Remove stored session for given identifier and auth.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error removing the session data.
         */
        public async Task removeSession(string identifier, PermissionLevel auth)
        {
            if (this.storage == null)
            {
                throw new Exception("Unable to remove session: No storage adapter configured");
            }

            var key = this.sessionKey(identifier, LinkConstants.formatAuth(auth));
            await this.storage.remove(key);
            await this.touchSession(identifier, auth, true);
        }

        /**
         * Remove all stored sessions for given identifier.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error removing the session data.
         */
        public async void clearSessions(string identifier)
        {
            if (this.storage == null)
            {
                throw new Exception("Unable to clear sessions: No storage adapter configured");
            }

            foreach (var auth in await this.listSessions(identifier))
            {
                await this.removeSession(identifier, auth);
            }
        }

        /**
         * Create an eosjs compatible signature provider using this anchorLink.
         * @param availableKeys Keys the created provider will claim to be able to sign for.
         * @param transport (internal) Transport override for this call.
         * @note We don't know what keys are available so those have to be provided,
         *       to avoid this use [[LinkSession.makeSignatureProvider]] instead. Sessions can be created with [[AnchorLink.login]].
         */
        public LinkSignatureProvider makeSignatureProvider(string[] availableKeys, ILinkTransport transport)
        {
            return new LinkSignatureProvider()
            {
                availableKeys = availableKeys,
                transport = transport ?? this.transport
            };
        }

        /** Makes sure session is in storage list of sessions and moves it to top (most recently used). */
        private async Task touchSession(string identifier, PermissionLevel auth, bool remove = false)
        {
            var auths = await this.listSessions(identifier);
            var formattedAuth = LinkConstants.formatAuth(auth);
            var existing = auths.IndexOf(auths.SingleOrDefault(a => LinkConstants.formatAuth(a) == formattedAuth));
            if (existing >= 0)
            {
                auths.RemoveAt(existing);
            }

            if (remove == false)
            {
                auths.Insert(0, auth);
            }

            var key = this.sessionKey(identifier, "list");
            await this.storage.write(key, JsonConvert.SerializeObject(auths));
        }

        /** Makes sure session is in storage list of sessions and moves it to top (most recently used). */
        private async Task storeSession(string identifier, LinkSession session)
        {
            var key = this.sessionKey(identifier, LinkConstants.formatAuth(session.auth));
            var data = JsonConvert.SerializeObject(session.serialize());
            await this.storage.write(key, data);
            await this.touchSession(identifier, session.auth);
        }

        /** Session storage key for identifier and suffix. */
        private string sessionKey(string identifier, string suffix)
        {
            return $"{this.chainId}-{identifier}-{suffix}";
        }

        /**
         * Connect to a WebSocket channel and wait for a message.
         * @internal
         */
        public Task<CallbackPayload> waitForCallback(string url, CancellationTokenSource cts)
        {
            return Task.Run(async () =>
            {
                var active = true;
                var retries = 0;
                string socketUrl = url.Replace("http", "ws");
                
                Console.WriteLine(socketUrl);

                CallbackPayload cbp = null;

                var socket = WebSocketWrapper.Create(socketUrl);
 
                socket.OnMessage += async (data) =>
                {
                    try
                    {
                        active = false;
                        if (socket.State == WebSocketState.Open)
                        {
                            //await socket.CloseAsync(WebSocketCloseStatus.Empty, "", cts.Token);
                        }

                        cbp = JsonConvert.DeserializeObject<CallbackPayload>(data);
                        if (cbp.data == null)
                            cbp.data = new Dictionary<string, string>();
                    }
                    catch (Exception ex)
                    {
                        cts.Cancel();
                        Console.WriteLine(data.ToString());
                        throw new Exception("Unable to parse callback JSON: " + ex.Message);
                    }
                };
                socket.OnOpen += () =>
                {
                    Console.WriteLine($"connected");
                    retries = 0;
                };
                //socket.OnError += Console.WriteLine;
                socket.OnClose += async (code, closeReason) =>
                {
                    Console.WriteLine($"closed {code} {closeReason}");
                    if (active)
                    {
                        // I have no idea if this backoff-thing makes sense :D
                        await Task.Delay(100);
                        await socket.ConnectAsync();
                    }
                };

                await socket.ConnectAsync();
                while (cbp == null && !cts.IsCancellationRequested && retries < 100)
                {
                    var test = socket.State;
                    Console.WriteLine(socket.State);

                    await Task.Delay(100, cts.Token);
                }

                active = false;
                return cbp;

            }, cts.Token);
        }

        public void pollForCallback(string url, CancellationToken ctl)
        {
            Task.Run(async () =>
            {
                var active = true;
                while (active)
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var response = await httpClient.GetAsync(new Uri(url), ctl);
                            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                            {
                                continue;
                            }
                            else if (response.StatusCode == HttpStatusCode.OK)
                            {
                                /*return */
                                Console.WriteLine(await response.Content.ReadAsStringAsync());
                            }
                            else
                            {
                                throw new Exception($"HTTP {response.StatusCode}: {response.ReasonPhrase}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected hyperbuoy error {ex.Message}");
                    }

                    await Task.Delay(1000, ctl);
                }
            }, ctl);
        }
    }
}