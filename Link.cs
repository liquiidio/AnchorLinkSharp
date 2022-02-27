using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EosioSigningRequest;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using WebSocketState = HybridWebSocket.WebSocketState;
using WebSocket = HybridWebSocket.WebSocket;
using EosSharp.Core.Providers;
using Transaction = System.Transactions.Transaction;
using EosSharp;
using Newtonsoft.Json;

namespace AnchorLinkSharp
{
    public static class LinkConstants
    {
        /**
         * Exponential backoff function that caps off at 10s after 10 tries.
         * https://i.imgur.com/IrUDcJp.png
         * @internal
         */
        public static double backoff(int tries) // TODO double or int?
        {
            return Math.Min(Math.Pow(tries * 10, 2), 10 * 1000);
        }

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
     * Payload accepted by the [[Link.transact]] method.
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
     * Options for the [[Link.transact]] method.
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
     * The result of a [[Link.transact]] call.
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
     * The result of a [[Link.identify]] call.
     */
    public class IdentifyResult : TransactResult {
        /** The identified account. */
        public object account { get; set; }

        /** The public key that signed the identity proof.  */
        public string signerKey { get; set; }
    }

    /**
     * The result of a [[Link.login]] call.
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
     * import AnchorLink from 'anchor-link'
     * import ConsoleTransport from 'anchor-link-console-transport'
     *
     * const link = new AnchorLink({
     *     transport: new ConsoleTransport()
     * })
     *
     * const result = await link.transact({actions: myActions})
     * ```
     */
    public class Link : IAbiProvider
    {
        /** The eosjs RPC instance used to communicate with the EOSIO node. */
        public readonly EosApi rpc;

        /** Transport used to deliver requests to the user wallet. */
        public readonly ILinkTransport transport;

        /** EOSIO ChainID for which requests are valid. */
        public readonly string chainId;

        /** Storage adapter used to persist sessions. */
        public readonly ILinkStorage storage;

        private string serviceAddress;
        private SigningRequestEncodingOptions requestOptions;

        private Dictionary<string, Abi> abiCache = new Dictionary<string, Abi>();
        private Dictionary<string, Task<GetAbiResponse>> pendingAbis = new Dictionary<string, Task<GetAbiResponse>>();

        /** Create a new link instance. */
        public Link(ILinkOptions options)
        {

            if (options.transport == null)
            {
                throw new Exception("options.transport is required, see https://github.com/greymass/anchor-link#transports");
            }

            if (options.chainId != null)
            {
                this.chainId = options.chainId is long
                    ? Constants.nameToId((long) options.chainId)
                    : (string) options.chainId;
            }
            else
            {
                this.chainId = Defaults.chainId;
            }

            if (options.rpc is string && !string.IsNullOrEmpty((string) options.rpc))
            {
                this.rpc = new EosApi(new EosConfigurator()
                {
                    ChainId = this.chainId,
                    ExpireSeconds = 10,
                    HttpEndpoint = (string) options.rpc,
                }, new HttpHandler());
            }
            else
            {
                this.rpc = (EosApi) options.rpc;
            }

            this.serviceAddress = (options.service ?? Defaults.service).Trim(); //.replace(/\/$/, '') TODO
            this.transport = options.transport;
            if (options.storage != null)
            {
                this.storage = options.storage ?? this.transport.storage;
            }

            this.requestOptions = new SigningRequestEncodingOptions()
            {
                abiProvider = this,
                zlib = null // TODO
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
                    getAbi = this.rpc.GetAbi(new GetAbiRequest() {account_name = account});
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
         * Create a SigningRequest instance configured for this link.
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
                    callback = new LinkCallback()
                    {
                        url = this.createCallbackUrl(),
                        background = true
                    }
                },
                this.requestOptions
            );
            request = await t.prepare(request);

            return request;
        }

        /**
         * Send a SigningRequest instance using this link.
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
                    throw new Exception("Request must have a link callback");
                }

                if (request.data.flags != 2)
                {
                    throw new Exception("Invalid request flags");
                }

                // wait for callback or user cancel
                var cts = new CancellationTokenSource();
                var socket = await waitForCallback(linkUrl, cts).ContinueWith((data) =>
                {
                    if (data.IsCanceled)
                    {
                        throw new CancelException($"Task cancelled");
                    }

                    if (data.Exception != null)
                    {
                        throw new CancelException($"Rejected by wallet: {data.Exception.Message}");
                    }

                    return data;
                }, cts.Token);
                var token = cts.Token;

                var cancel = new Task(() =>
                {
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
                }, token);

                Task.WaitAny(new[] {socket, cancel});
                CallbackPayload payload = new CallbackPayload(); // Todo -> socket.Result ?
                PermissionLevel signer = new PermissionLevel()
                {
                    actor = payload.sa,
                    permission = payload.sp,
                };

                List<string> signatures = payload.sigs.Values.ToList(); // TODO, this is not part of the original object

                // recreate transaction from request response
                var resolved = await ResolvedSigningRequest.fromPayload(
                    payload,
                    this.requestOptions,
                    this
                );
                var info = resolved.request.getInfo();
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
                    var res = await this.rpc.PushTransaction(new PushTransactionRequest()
                    {
                        signatures = result.signatures,
                        transaction = transaction,
                        compression = 0, // TOOD ?
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
        public async Task<TransactResult> transact(TransactArgs args, TransactOptions options, ILinkTransport transport)
        {
            var t = transport ?? this.transport;
            var broadcast = options == null || options.broadcast;
            // Initialize the loading state of the transport
            t.showLoading();

            // eosjs transact compat: upgrade to transaction if args have any header field
            // TODO
            if (args.action != null || args.actions != null)
            {
                args.transaction = new EosSharp.Core.Api.v1.Transaction()
                {
                    expiration = new DateTime(1970, 1, 1),
                    ref_block_num = 0,
                    ref_block_prefix = 0,
                    max_net_usage_words = 0,
                    max_cpu_usage_ms = 0,
                    delay_sec = 0
                };
            }

            SigningRequestCreateArguments signingRequestCreateArguments = new SigningRequestCreateArguments()
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
         * @note This is for advanced use-cases, you probably want to use [[Link.login]] instead.
         */
        public async Task<IdentifyResult> identify(PermissionLevel requestPermission, object info /*, info?: {[key: string]: string | Uint8Array}*/)
        {
            var request = await this.createRequest(new SigningRequestCreateArguments()
            {
                identity = new Identity()
                {
                    permission = requestPermission,
                },
                info = info
            });

            var res = await this.sendRequest(request, null); // TODO
            if (!res.request.isIdentity())
            {
                throw new IdentityException("Unexpected response");
            }

            MemoryStream memStream = new MemoryStream();

            var chainIdBuff = SerializationHelper.HexStringToByteArray(request.getChainId());
            memStream.Write(chainIdBuff, 0, chainIdBuff.Length);
            memStream.Write(res.serializedTransaction, chainIdBuff.Length, res.serializedTransaction.Length);
            memStream.Write(new byte[32], chainIdBuff.Length + res.serializedTransaction.Length, 32);
            var message = memStream.ToArray();  // TODO

            var signer = res.signer;
            string signerKey = ""; //  ecc.recover(res.signatures[0], message)   // TODO
            var account = await this.rpc.GetAccount(new GetAccountRequest() {account_name = signer.actor});
            if (account == null)
            {
                throw new IdentityException($"Signature from unknown account: {signer.actor}");
            }

            var permission = account.permissions.SingleOrDefault(p => p.perm_name == signer.permission);
            if (permission == null)
            {
                throw new IdentityException($"{signer.actor} signed for unknown permission: {signer.permission}");
            }

            var auth = permission.required_auth;
            var keyAuth = auth.keys.SingleOrDefault(key => key.key == signerKey); // TODO key-equal-func
            if (keyAuth == null)
            {
                throw new IdentityException($"{LinkConstants.formatAuth(signer)} has no key matching id signature");
            }

            if (auth.threshold > keyAuth.weight)
            {
                throw new IdentityException(
                    $"{LinkConstants.formatAuth(signer)} signature does not reach auth threshold");
            }

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
                signerKey = signerKey
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
            var res = await this.identify(null, LinkUtils.abiEncode(createInfo, "link_create"));
            var metadata = new Dictionary<string, object>() {{"sameDevice", res.request.getRawInfo()["return_path"]}};
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
         * Restore previous session, see [[Link.login]] to create a new session.
         * @param identifier The session identifier, should be same as what was used when creating the session with [[Link.login]].
         * @param auth A specific session auth to restore, if omitted the most recently used session will be restored.
         * @returns A [[LinkSession]] instance or null if no session can be found.
         * @throws If no [[LinkStorage]] adapter is configured or there was an error retrieving the session data.
         **/
        public async Task<LinkSession> restoreSession(string identifier, PermissionLevel auth)
        {
            if (this.storage == null)
            {
                throw new Exception("Unable to restore session: No storage adapter configured");
            }

            string key;
            if (auth != null)
            {
                key = this.sessionKey(identifier, LinkConstants.formatAuth(auth));
            }
            else
            {
                var latest = (await this.listSessions(identifier))[0];
                if (latest == null)
                {
                    return null;
                }

                key = this.sessionKey(identifier, LinkConstants.formatAuth(latest));
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
            var list = new List<PermissionLevel>();
            try
            {
                list = JsonConvert.DeserializeObject<List<PermissionLevel>>((await this.storage.read(key)) ?? "{}");
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
         * Create an eosjs compatible signature provider using this link.
         * @param availableKeys Keys the created provider will claim to be able to sign for.
         * @param transport (internal) Transport override for this call.
         * @note We don't know what keys are available so those have to be provided,
         *       to avoid this use [[LinkSession.makeSignatureProvider]] instead. Sessions can be created with [[Link.login]].
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
        public async Task<CallbackPayload> waitForCallback(string url, CancellationTokenSource cts)
        {
            return await new Task<CallbackPayload>(() =>
            {
                var active = true;
                var retries = 0;
                string socketUrl = url.Replace("http", "ws");

                CallbackPayload cbp = null;

                WebSocket socket = new WebSocket(socketUrl);
                socket.OnMessage += (data) =>
                {
                    active = false;
                    if (socket.GetState() == WebSocketState.Open)
                    {
                        socket.Close();
                    }

                    try
                    {
                        cbp = JsonConvert.DeserializeObject<CallbackPayload>(data.ToString());
                    }
                    catch (Exception ex)
                    {
                        ex = new Exception("Unable to parse callback JSON: " + ex.Message);
                        cts.Cancel();
                    }
                };
                socket.OnOpen += () => { retries = 0; };
                socket.OnError += msg => { };
                socket.OnClose += (code) =>
                {
                    if (active)
                    {
                        // I have no idea if this backoff-thing makes sense :D
                        LinkConstants.backoff(retries++);
                        Task.Delay(100);
                        socket.Connect();
                    }
                };

                while (cbp == null && !cts.IsCancellationRequested)
                {
                    Task.Delay(100, cts.Token);
                }
                return cbp;

            }, cts.Token);
        }
    }
}