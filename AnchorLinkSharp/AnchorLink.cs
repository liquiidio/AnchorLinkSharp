using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace AnchorLinkSharp
{
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
        public readonly ILinkTransport Transport;

        /** EOSIO ChainID for which requests are valid. */
        public readonly string ChainId;

        /** PlayerPrefsStorage adapter used to persist sessions. */
        public readonly ILinkStorage Storage;

        private readonly string _serviceAddress;
        private readonly SigningRequestEncodingOptions _requestOptions;

        private readonly Dictionary<string, Abi> _abiCache = new Dictionary<string, Abi>();
        private readonly Dictionary<string, Task<GetAbiResponse>> _pendingAbis = new Dictionary<string, Task<GetAbiResponse>>();

        /** Create a new anchorLink instance. */
        public AnchorLink(ILinkOptions options)
        {
            
            if (options.Transport == null)
            {
                throw new Exception("options.transport is required, see https://github.com/greymass/anchor-anchorLink#transports");
            }

            if (options.ZlibProvider == null)
            {
                throw new Exception("options.ZlibProvider is required");
            }

            if (options.ChainId == null)
            {
                ChainId = Defaults.ChainId;
            }

            if (options.Rpc is string && !string.IsNullOrEmpty((string) options.Rpc))
            {
                Api = new EosApi(new EosConfigurator()
                {
                    ChainId = ChainId,
                    ExpireSeconds = 10,
                    HttpEndpoint = (string) options.Rpc,
                }, new HttpHandler());
            }
            else if(options.Rpc is EosApi eosApi)
            {
                Api = eosApi;
            }

            _serviceAddress = (options.Service ?? Defaults.Service).Trim(); //.replace(/\/$/, '') TODO
            Transport = options.Transport;
            if (options.Storage != null)
            {
                Storage = options.Storage ?? Transport.Storage;
            }

            _requestOptions = new SigningRequestEncodingOptions()
            {
                AbiSerializationProvider = this,
                SignatureProvider = new DefaultSignProvider(),
                Zlib = options.ZlibProvider
            };
        }


        /**
         * Fetch the ABI for given account, cached.
         * @internal
         */
        public async Task<Abi> GetAbi(string account)
        {
            var rv = _abiCache[account];
            if (rv == null)
            {
                var getAbi = _pendingAbis[account];
                if (getAbi == null)
                {
                    getAbi = Api.GetAbi(new GetAbiRequest() {account_name = account});
                    _pendingAbis.Add(account, getAbi);
                }

                rv = (await getAbi).abi;
                _pendingAbis.Remove(account);
                if (rv != null)
                {
                    _abiCache.Add(account, rv);
                }
            }

            return rv;
        }

        /**
         * Create a new unique buoy callback url.
         * @internal
         */
        public string CreateCallbackUrl()
        {
            return $"{_serviceAddress}/{Guid.NewGuid()}";
        }

        /**
         * Create a SigningRequest instance configured for this anchorLink.
         * @internal
         */
        public async Task<SigningRequest> CreateRequest(SigningRequestCreateArguments args, ILinkTransport transport = null)
        {
            var t = transport ?? Transport;
            // generate unique callback url
            var request = await SigningRequest.Create(
                new SigningRequestCreateArguments()
                {
                    Action = args.Action,
                    Transaction = args.Transaction,
                    ChainId = ChainId,
                    Broadcast = false,
                    Callback = new Dictionary<string, object>()
                    {
                        {"url", CreateCallbackUrl()},
                        {"background", true},
                    },
                    Identity = args.Identity
                },
                _requestOptions
            );
            request = await t.Prepare(request);

            return request;
        }

        /**
         * Send a SigningRequest instance using this anchorLink.
         * @internal
         */
        public async Task<TransactResult> SendRequest(SigningRequest request, ILinkTransport transport,
            bool broadcast = false)
        {
            var t = transport ?? Transport;
            try
            {
                var linkUrl = request.Data.Callback;
                if (!linkUrl.StartsWith(_serviceAddress))
                {
                    throw new Exception("Request must have a anchor.link callback");
                }

                if (request.Data.Flags != 2)
                {
                    throw new Exception("Invalid request flags");
                }

                // wait for callback or user cancel
                var cts = new CancellationTokenSource();
                var socket = WaitForCallback(linkUrl, cts);
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
                t.OnRequest(request, (reason) =>
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
                    actor = payload.Sa,
                    permission = payload.Sp,
                };

                List<string> signatures = new List<string>(){ payload.Sig }; // TODO, multiple sigs?

                // recreate transaction from request response
                var resolved = await ResolvedSigningRequest.FromPayload(
                    payload,
                    _requestOptions,
                    this
                );
                var info = resolved.Request.GetInfos();
                if (info.ContainsKey("fuel_sig"))
                {
                    signatures.Insert(0, info["fuel_sig"]);
                }

                var transaction = resolved.Transaction;
                var serializedTransaction = resolved.SerializedTransaction;
                TransactResult result = new TransactResult()
                {
                    Request = resolved.Request,
                    SerializedTransaction = serializedTransaction,
                    Transaction = transaction,
                    Signatures = signatures.ToArray(),
                    Payload = payload,
                    Signer = signer,
                };
                if (broadcast)
                {
                    var res = await Api.PushTransaction(new PushTransactionRequest()
                    {
                        signatures = result.Signatures,
                        transaction = transaction,
                        compression = 0, // TODO ?
                        // TODO ! pass other properties
                    });
                    result.Processed = res.processed;
                }

                t.OnSuccess(request, result);

                return result;
            }
            catch (Exception ex)
            {
                t.OnFailure(request, ex);
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
        public async Task<TransactResult> Transact(TransactArgs args, TransactOptions options = null, ILinkTransport transport = null)
        {
            var t = transport ?? Transport;
            var broadcast = options == null || options.Broadcast;
            // Initialize the loading state of the transport
            t.ShowLoading();

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
                Transaction = args.Transaction,
                Action = args.Action,
                Actions = args.Actions,
                ChainId = ChainId,
                Broadcast = broadcast,
                Callback = CreateCallbackUrl()
            };

            var request = await CreateRequest(signingRequestCreateArguments, t);
            var result = await SendRequest(request, t, broadcast);
            return result;
        }

        /**
         * Send an identity request and verify the identity proof.
         * @param requestPermission Optional request permission if the request is for a specific account or permission.
         * @param info Metadata to add to the request.
         * @note This is for advanced use-cases, you probably want to use [[AnchorLink.login]] instead.
         */
        public async Task<IdentifyResult> Identify(/*TODO Scope */PermissionLevel requestPermission, object info /*, info?: {[key: string]: string | Uint8Array}*/)
        {
            var request = await CreateRequest(new SigningRequestCreateArguments()
            {
                Identity = new IdentityV2()
                {
                    Permission = requestPermission,
                },
                Info = info// (List<InfoPair>)info,
            });

            /*string test = "";
            pollForCallback(request.data.callback, CancellationToken.None);*/
            //pollForCallback(request.data.callback, CancellationToken.None);
            var res = await SendRequest(request, null); // TODO
            if (!res.Request.IsIdentity())
            {
                throw new IdentityException("Unexpected response");
            }

            //var memStream = new MemoryStream();

            //var chainIdBuff = SerializationHelper.HexStringToByteArray(request.getChainId());
            //memStream.Write(chainIdBuff, 0, chainIdBuff.Length);
            //memStream.Write(res.serializedTransaction, 0, res.serializedTransaction.Length);
            //memStream.Write(new byte[32], 0, 32);
            //var message = memStream.ToArray();  // TODO

            var signer = res.Signer;
            //string signerKey = ""; //  ecc.recover(res.signatures[0], message)   // TODO
            var account = await Api.GetAccount(new GetAccountRequest() {account_name = signer.actor});
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
                        $"Unexpected identity proof from {LinkConstants.FormatAuth(signer)}, expected {LinkConstants.FormatAuth(requestPermission)}");
                }
            }

            return new IdentifyResult()
            {
                Payload = res.Payload,
                Signatures = res.Signatures,
                Processed = res.Processed,
                Signer = res.Signer,
                Request = res.Request,
                Transaction = res.Transaction,
                SerializedTransaction = res.SerializedTransaction,
                Account = account,
                //signerKey = signerKey
            };
        }

        /**
         * Login and create a persistent session.
         * @param identifier The session identifier, an EOSIO name (`[a-z1-5]{1,12}`).
         *                   Should be set to the contract account if applicable.
         */
        public async Task<LoginResult> Login(string identifier)
        {
            var keyPair = CryptoHelper.GenerateKeyPair();
            var privateKey = keyPair.PrivateKey;
            var requestKey = keyPair.PublicKey;
            var createInfo = new LinkCreate()
            {
                SessionName = identifier,
                RequestKey = requestKey,
            };
            _requestOptions.SignatureProvider = new DefaultSignProvider(privateKey);

            var res = await Identify(Constants.PlaceholderAuth, LinkUtils.AbiEncode(createInfo, "link_create"));
            var metadata = new Dictionary<string, object>();
            var rawInfo = res.Request.GetRawInfo();
            if(rawInfo.ContainsKey("return_path"))
                metadata.Add("sameDevice", rawInfo["return_path"]);
            LinkSession session;
            if (res.Payload.Data.ContainsKey("link_ch") && res.Payload.Data.ContainsKey("link_key") &&
                res.Payload.Data.ContainsKey("link_name"))
            {
                session = new LinkChannelSession(
                    this, new LinkChannelSessionData()
                    {
                        Identifier = identifier,
                        Auth = res.Signer,
                        PublicKey = res.SignerKey,
                        Channel = new ChannelInfo()
                        {
                            Url = res.Payload.Data["link_ch"],
                            Key = res.Payload.Data["link_key"],
                            Name = res.Payload.Data["link_name"]
                        },
                        RequestKey = privateKey
                    },
                    metadata
                );
            }
            else
            {
                session = new LinkFallbackSession(
                    this, new LinkFallbackSessionData()
                    {
                        Identifier = identifier,
                        Auth = new PermissionLevel()
                        {
                            actor = res.Signer.actor,
                            permission = res.Signer.permission
                        }
                    },
                    metadata
                );
            }

            if (Storage != null)
            {
                await StoreSession(identifier, session);
            }

            return new LoginResult
            {
                Payload = res.Payload,
                Transaction = res.Transaction,
                Signatures = res.Signatures,
                SignerKey = res.SignerKey,
                Account = res.Account,
                SerializedTransaction = res.SerializedTransaction,
                Signer = res.Signer,
                Processed = res.Processed,
                Request = res.Request,
                Session = session
            };
        }

        /**
         * Restore previous session, see [[AnchorLink.login]] to create a new session.
         * @param identifier The session identifier, should be same as what was used when creating the session with [[AnchorLink.login]].
         * @param auth A specific session auth to restore, if omitted the most recently used session will be restored.
         * @returns A [[LinkSession]] instance or null if no session can be found.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error retrieving the session data.
         **/
        public async Task<LinkSession> RestoreSession(string identifier, PermissionLevel auth = null)
        {
            if (Storage == null)
            {
                throw new Exception("Unable to restore session: No storage adapter configured");
            }

            string key = "";
            if (auth != null)
            {
                key = SessionKey(identifier, LinkConstants.FormatAuth(auth));
            }
            else
            {
                var latestPermissions = (await ListSessions(identifier));
                if (latestPermissions.Count > 0)
                {
                    var latest = latestPermissions[0];
                    if (latest == null)
                    {
                        return null;
                    }

                    key = SessionKey(identifier, LinkConstants.FormatAuth(latest));
                }
            }

            var data = await Storage.Read(key);
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

            var session = LinkSession.Restore(this, sessionData);
            if (auth != null)
            {
                // update latest used
                await TouchSession(identifier, auth);
            }

            return session;
        }

        /**
         * List stored session auths for given identifier.
         * The most recently used session is at the top (index 0).
         * @throws If no [[LinkStorage]] adapter is configured or there was an error retrieving the session list.
         **/
        public async Task<List<PermissionLevel>> ListSessions(string identifier)
        {
            if (Storage == null)
            {
                throw new Exception("Unable to list sessions: No storage adapter configured");
            }

            var key = SessionKey(identifier, "list");
            List<PermissionLevel> list;
            try
            {
                list = JsonConvert.DeserializeObject<List<PermissionLevel>>((await Storage.Read(key)) ?? "{}") ?? new List<PermissionLevel>();
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
        public async Task RemoveSession(string identifier, PermissionLevel auth)
        {
            if (Storage == null)
            {
                throw new Exception("Unable to remove session: No storage adapter configured");
            }

            var key = SessionKey(identifier, LinkConstants.FormatAuth(auth));
            await Storage.Remove(key);
            await TouchSession(identifier, auth, true);
        }

        /**
         * Remove all stored sessions for given identifier.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error removing the session data.
         */
        public async void ClearSessions(string identifier)
        {
            if (Storage == null)
            {
                throw new Exception("Unable to clear sessions: No storage adapter configured");
            }

            foreach (var auth in await ListSessions(identifier))
            {
                await RemoveSession(identifier, auth);
            }
        }

        /**
         * Create an eosjs compatible signature provider using this anchorLink.
         * @param availableKeys Keys the created provider will claim to be able to sign for.
         * @param transport (internal) Transport override for this call.
         * @note We don't know what keys are available so those have to be provided,
         *       to avoid this use [[LinkSession.makeSignatureProvider]] instead. Sessions can be created with [[AnchorLink.login]].
         */
        public LinkSignatureProvider MakeSignatureProvider(string[] availableKeys, ILinkTransport transport)
        {
            return new LinkSignatureProvider()
            {
                AvailableKeys = availableKeys,
                Transport = transport ?? Transport
            };
        }

        /** Makes sure session is in storage list of sessions and moves it to top (most recently used). */
        private async Task TouchSession(string identifier, PermissionLevel auth, bool remove = false)
        {
            var auths = await ListSessions(identifier);
            var formattedAuth = LinkConstants.FormatAuth(auth);
            var existing = auths.IndexOf(auths.SingleOrDefault(a => LinkConstants.FormatAuth(a) == formattedAuth));
            if (existing >= 0)
            {
                auths.RemoveAt(existing);
            }

            if (remove == false)
            {
                auths.Insert(0, auth);
            }

            var key = SessionKey(identifier, "list");
            await Storage.Write(key, JsonConvert.SerializeObject(auths));
        }

        /** Makes sure session is in storage list of sessions and moves it to top (most recently used). */
        private async Task StoreSession(string identifier, LinkSession session)
        {
            var key = SessionKey(identifier, LinkConstants.FormatAuth(session.Auth));
            var data = JsonConvert.SerializeObject(session.Serialize());
            await Storage.Write(key, data);
            await TouchSession(identifier, session.Auth);
        }

        /** Session storage key for identifier and suffix. */
        private string SessionKey(string identifier, string suffix)
        {
            return $"{ChainId}-{identifier}-{suffix}";
        }

        /**
         * Connect to a WebSocket channel and wait for a message.
         * @internal
         */
        public Task<CallbackPayload> WaitForCallback(string url, CancellationTokenSource cts)
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
                        if (cbp.Data == null)
                            cbp.Data = new Dictionary<string, string>();
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

        public void PollForCallback(string url, CancellationToken ctl)
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