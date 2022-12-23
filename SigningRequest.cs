/**
 * EOSIO Signing Request (ESR).
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EosSharp;
using Action = EosSharp.Core.Api.v1.Action;

namespace EosioSigningRequest
{
    public partial class SigningRequest
    {
        public static readonly AbiStruct Type = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "signing_request");
        public static readonly AbiStruct TypeV3 = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "signing_request_identity_v3");
        public static AbiStruct IdType = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "identity");
        public static AbiStruct TransactionType = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "transaction");

        /** Create a new signing request. */
        public static async Task<SigningRequest> Create(SigningRequestCreateArguments args, SigningRequestEncodingOptions options)
        {
            byte version = 2;

            async Task<Action> Serialize(Action action)
            {
                if (string.IsNullOrEmpty(action.hex_data))
                {
                    var abi = await options.AbiSerializationProvider.GetAbi(action.account);
                    action.hex_data =
                        SerializationHelper.ByteArrayToHexString(
                            options.AbiSerializationProvider
                                .SerializeActionData(action, abi));
                }
                return action;
            }

            var data = new SigningRequestData();

            // set the request data
            if (args.Identity != null)
            {
                if (args.Identity is IdentityV3)
                {
                    version = 3;
                    data.Req = new KeyValuePair<string, object>("identity_v3", args.Identity);
                }
                else
                {
                    data.Req = new KeyValuePair<string, object>("identity", args.Identity);
                }
            }
            else if (args.Action != null && args.Actions == null && args.Transaction == null)
            {
                data.Req = new KeyValuePair<string, object>("action", await Serialize(args.Action));
            }
            else if (args.Actions != null && args.Action == null && args.Transaction == null)
            {
                if (args.Actions.Length == 1)
                {
                    data.Req = new KeyValuePair<string, object>("action", await Serialize(args.Actions[0]));
                }
                else
                {
                    data.Req = new KeyValuePair<string, object>("action[]", args.Actions.Select(async action => await Serialize(action)).Select(t => t.Result).ToArray());
                }
            }
            else if (args.Transaction != null && args.Action == null && args.Actions == null)
            {
                var tx = args.Transaction;
                // set default values if missing

                if (tx.context_free_actions == null)
                {
                    tx.context_free_actions = new List<Action>();
                }

                if (tx.transaction_extensions == null)
                {
                    tx.transaction_extensions = new List<Extension>();
                }

                // encode actions if needed
                tx.actions = tx.actions.Select(async action => await Serialize(action)).Select(t => t.Result).ToList();
                data.Req = new KeyValuePair<string, object>("transaction", tx);
            }
            else
            {
                throw new Exception("Invalid arguments: Must have exactly one of action, actions or transaction");
            }

            // set the chain id
            data.ChainId = SigningRequestConstants.VariantId(args.ChainId);
            data.Flags = AbiConstants.RequestFlagsNone;

            var broadcast = args.Broadcast ?? data.Req.Key != "identity" && data.Req.Key != "identity_v3";
            if (broadcast)
            {
                data.Flags |= AbiConstants.RequestFlagsBroadcast;
            }

            if (args.Callback is string callback)
            {
                data.Callback = callback;
            }
            else if(args.Callback is Dictionary<string, object> callbackDict)
            {
                if (callbackDict.TryGetValue("url", out var url) && url is string stringUrl)
                    data.Callback = stringUrl;
                if(callbackDict.TryGetValue("background",out var brdcst) && brdcst is bool brdcstBool)
                    data.Flags |= AbiConstants.RequestFlagsBackground;
            }
            else if (args.Callback is KeyValuePair<string, bool> obj)
            {
                data.Callback = obj.Key;
                if (obj.Value)
                {
                    data.Flags |= AbiConstants.RequestFlagsBackground;
                }
            }
            else {
                data.Callback = "";
            }

            data.Info = new List<object>();
            if (args.Info != null && args.Info is List<InfoPair> infoPairs)
                foreach (var infoPair in infoPairs)
                {
                    if (infoPair.Value is string stringVal)
                        data.Info.Add(new InfoPair(infoPair.Key, options.AbiSerializationProvider.Serialize(stringVal, "string")));
                    else
                        data.Info.Add(infoPair);
                }
            if (args.Info is Dictionary<string, string> dictionary) {
                foreach (var info in dictionary)
                {
                    data.Info.Add(new InfoPair()
                    {
                        Key = info.Key,
                        Value = info.Value
                    });
                }
            }

            var req = new SigningRequest( 
                version,
                data,
                options.Zlib,
                options.AbiSerializationProvider,
                null
            );

            // sign the request if given a signature provider
            if (options.SignatureProvider != null)
            {
                req.Sign(options.SignatureProvider);
            }

            return req;
        }

        /** Creates an identity request. */
        public static async Task<SigningRequest> Identity(SigningRequestCreateIdentityArguments args, SigningRequestEncodingOptions options)
        {
            var permission = new PermissionLevel()
            {
                actor = args.Account ?? SigningRequestConstants.PlaceholderName,
                permission = args.Permission ?? SigningRequestConstants.PlaceholderPermission
            };

            if (permission.actor == SigningRequestConstants.PlaceholderName &&
                permission.permission == SigningRequestConstants.PlaceholderPermission)
            {
                // TODO ?
//                permission = null;
            }
            return await Create(new SigningRequestCreateArguments()
            {
                Identity = new IdentityV2(){ Permission = permission },
                Broadcast = false,
                Callback = args.Callback,
                Info = args.Info
            }, options);
        }

        /**
         * Create a request from a chain id and serialized transaction.
         * @param chainId The chain id where the transaction is valid.
         * @param serializedTransaction The serialized transaction.
         * @param options Creation options.
         */
        public static SigningRequest FromTransaction(object chainId, object serializedTransaction, SigningRequestEncodingOptions options) 
        {
            if (chainId is byte[] byteId)
            {
                chainId = SerializationHelper.ByteArrayToHexString(byteId);
            }
            if (serializedTransaction is string transaction)
            {
                serializedTransaction = SerializationHelper.HexStringToByteArray(transaction);
            }

            using (var buf = new MemoryStream())
            {
                buf.WriteByte(2); // header
                var id= SigningRequestConstants.VariantId(chainId);
                if (id.Key == "chain_alias")
                {
                    buf.WriteByte(0);
                    buf.WriteByte(Convert.ToByte((int)id.Value));
                }
                else
                {
                    buf.WriteByte(1);
                    var bytes = SerializationHelper.HexStringToByteArray((string)id.Value);
                    buf.Write(bytes, 0, bytes.Length);
                }

                buf.WriteByte(2); // transaction variant
                buf.Write((byte[])serializedTransaction,0, ((byte[])serializedTransaction).Length);
                buf.WriteByte(AbiConstants.RequestFlagsBroadcast); // flags
                buf.WriteByte(0); // callback
                buf.WriteByte(0); // info

                return FromData(buf.ToArray(), options);
            }
        }

        /** Creates a signing request from encoded `esr:` uri string. */
        public static SigningRequest From(string uri, SigningRequestEncodingOptions options) {
            //const [scheme, path] = uri.split(':')
            var subs = uri.Split(':');
            var scheme = subs[0];
            var path = subs[1];
            if (scheme != "esr" && scheme != "web+esr")
            {
                throw new Exception("Invalid scheme");
            }

            path = (path.StartsWith("//") ? path.Substring(2) : path)
                .Replace('-', '+')
                .Replace('_', '/');

            if (path.Length % 4 == 2)
                path = $"{path}==";
            else if(path.Length % 4 == 3)
                path = $"{path}=";

            var data = Convert.FromBase64String(path);

            return FromData(data, options);
        }

        public static SigningRequest FromData(byte[] data, SigningRequestEncodingOptions options ) {
            var header  = data[0];
            var version = (byte)(header & ~(1 << 7));

            var array = new byte[data.Length-1];
            Array.Copy(data, 1, array, 0, data.Length-1);
            
            if ((header & (1 << 7)) != 0)
            {
                if (options.Zlib == null)
                {
                    throw new Exception("Compressed URI needs zlib");
                }

                array = options.Zlib.InflateRaw(array);
            }


            var readIndex = 0;
            SigningRequestData requestData = null;
            if(version == 3)
                requestData = options.AbiSerializationProvider.DeserializeStructData<SigningRequestData>("signing_request_identity_v3", array, SigningRequestAbi.Abi, ref readIndex);
            else
                requestData = options.AbiSerializationProvider.DeserializeStructData<SigningRequestData>("signing_request", array, SigningRequestAbi.Abi, ref readIndex);

            if (requestData.Req.Key == "action" && requestData.Req.Value is Dictionary<string, object> valueDict)
            {
                if (valueDict.ContainsKey("data") && valueDict["data"] is byte[] bytes)
                {
                    valueDict.Add("hex_data", SerializationHelper.ByteArrayToHexString(bytes));
                }
            }

            RequestSignature signature = null;
            if (readIndex < array.Length)
            {
                signature = options.AbiSerializationProvider.DeserializeStructData<RequestSignature>("request_signature", array, SigningRequestAbi.Abi, ref readIndex);
            }

            return new SigningRequest(
                version,
                requestData,
                options.Zlib,
                options.AbiSerializationProvider,
                signature
            );
        }

        /** The signing request version. */
        public readonly byte Version = 2;

        /** The raw signing request data. */
        public readonly SigningRequestData Data;

        /** The request signature. */
        public RequestSignature Signature;

        private readonly IZlibProvider _zlib;
        private readonly IAbiSerializationProvider _abiSerializationProvider;

        /**
            * Create a new signing request.
            * Normally not used directly, see the `create` and `from` class methods.
            */
        public SigningRequest(byte version, 
            SigningRequestData data,
            IZlibProvider zlib,
            IAbiSerializationProvider abiSerializationProvider,
            RequestSignature signature
        ) 
        {
            if ((data.Flags & AbiConstants.RequestFlagsBroadcast) != 0 && (data.Req.Key == "identity" || data.Req.Key == "identity_v3"))
            {
                throw new Exception("Invalid request (identity request cannot be broadcast)");
            }

            if ((data.Flags & AbiConstants.RequestFlagsBroadcast) == 0 && data.Callback.Length == 0)
            {
                throw new Exception("Invalid request (nothing to do, no broadcast or callback set)");
            }

            Version = version;
            Data = data;
            _zlib = zlib;
            _abiSerializationProvider = abiSerializationProvider;
            Signature = signature;
        }

        /**
         * Sign the request, mutating.
         * @param signatureProvider The signature provider that provides a signature for the signer.
         */
        public void Sign(ISignProvider signatureProvider)
        {
            var message = GetSignatureDigest();
            var signatureData = signatureProvider.Sign(GetChainId(), message);
            Signature = new RequestSignature()
            {
                Signer = SigningRequestConstants.PlaceholderName,
                Signature = signatureData
            };
        }

        /**
         * Get the signature digest for this request.
         */
        public byte[] GetSignatureDigest()
        {

            // TODO, is the following correct?

            // protocol version + utf8 "request"
            byte[] versionUtf8 = {Version, 0x72, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74};
            var data = GetData();

            var req = new byte[versionUtf8.Length + data.Length];
            versionUtf8.CopyTo(req, 0);
            data.CopyTo(req, versionUtf8.Length);
            return Sha256Manager.GetHash(req);
        }

        /**
         * Set the signature data for this request, mutating.
         * @param signer Account name of signer.
         * @param signature The signature string.
         */
        public void SetSignature(string signer, string signature)
        {
            Signature = new RequestSignature()
            {
                Signer = signer,
                Signature = signature
            };
        }

        /**
         * Set the request callback, mutating.
         * @param url Where the callback should be sent.
         * @param background Whether the callback should be sent in the background.
         */
        public void SetCallback(string url, bool background)
        {
            Data.Callback = url;
            if (background)
            {
                Data.Flags |= AbiConstants.RequestFlagsBackground;
            }
            else
            {
                Data.Flags = ((byte)(Data.Flags &  ~AbiConstants.RequestFlagsBackground));
            }
        }

        /**
         * Set broadcast flag.
         * @param broadcast Whether the transaction should be broadcast by receiver.
         */
        public void SetBroadcast(bool broadcast)
        {
            if (broadcast)
            {
                Data.Flags |= AbiConstants.RequestFlagsBroadcast;
            }
            else
            {
                Data.Flags = ((byte)(Data.Flags & ~AbiConstants.RequestFlagsBroadcast));
            }
        }

        /*
         * Encode this request into an `esr:` uri.
         * @argument compress Whether to compress the request data using zlib,
         *                    defaults to true if omitted and zlib is present;
         *                    otherwise false.
         * @argument slashes Whether add slashes after the protocol scheme, i.e. `esr://`.
         *                   Defaults to true.
         * @returns An esr uri string.
         */
        static readonly char Padding =  '=';
        public string Encode(bool? compress = null, bool? slashes = null)
        {
            var shouldCompress = compress ?? _zlib != null;
            if (shouldCompress && _zlib == null)
            {
                throw new Exception("Need zlib to compress");
            }

            var header = Version;
            var data = GetData();
            var sigData = GetSignatureData();
            var array = new byte[data.Length + sigData.Length];
            data.CopyTo(array,0);
            sigData.CopyTo(array, data.Length);
            if (shouldCompress)
            {
                var deflated = _zlib!.DeflateRaw(array);
                if (array.Length > deflated.Length)
                {
                    header |= 1 << 7;
                    array = deflated;
                }
            }

            var output = new byte[1 + array.Length];
            output[0] = header;
            array.CopyTo(output, 1);
            var scheme = "esr:";
            if (slashes != false)
                scheme += "//";

            return $"{scheme}{Convert.ToBase64String(output).Replace('/', '_').Replace('+', '-').TrimEnd('=')}";
        }

        /** Get the request data without header or signature. */
        public byte[] GetData()
        {
            if (Data.Req.Key == "action")
            {
                if (Data.Req.Value is Action action)
                {
                    ((Action)Data.Req.Value).data = SerializationHelper.HexStringToByteArray(action.hex_data);
                }
                else if (Data.Req.Value is Dictionary<string, object> dict)
                {
                    var auth = (List<object>)dict["authorization"];

                    var authorization = new List<PermissionLevel>();
                    foreach (Dictionary<string, object> permLevel in auth)
                    {
                        authorization.Add(new PermissionLevel()
                            { actor = (string)permLevel["actor"], permission = (string)permLevel["permission"] });
                    }

                    Data.Req = new KeyValuePair<string, object>(Data.Req.Key,
                        new Action()
                        {
                            data = dict["data"],
                            authorization = authorization,
                            name = (string)dict["name"],
                            account = (string)dict["account"]
                        }
                    );
                }
                else
                {
                    throw new Exception($"type {Data.Req.Value.GetType().Name} not supported");
                }
            }
            else if (Data.Req.Key == "action[]")
            {
                throw new NotImplementedException();
            }
            else if (Data.Req.Key == "identity" || Data.Req.Key == "identity_v3")
            {

            }
            else
            {
                throw new NotSupportedException($"request key \"{Data.Req.Key}\" not supported");
            }
            

            if(Version == 2)
                return _abiSerializationProvider.SerializeStructData(Data, Type, SigningRequestAbi.Abi);
            if(Version == 3)
                return _abiSerializationProvider.SerializeStructData(Data, TypeV3, SigningRequestAbi.Abi);
            return Array.Empty<byte>();
        }

        /** Get signature data, returns an empty array if request is not signed. */
        public byte[] GetSignatureData() {
            if (Signature == null)
            {
                return Array.Empty<byte>();
            }

            var structType = SigningRequestAbi.Abi.structs.FirstOrDefault(t => t.name == "request_signature")!;
            return _abiSerializationProvider.SerializeStructData(Signature, structType, SigningRequestAbi.Abi);
        }

        /** ABI definitions required to resolve request. */
        public List<string> GetRequiredAbis()
        {
            return GetRawActions().Where(a => !SigningRequestConstants.IsIdentity(a)).Select(a => a.account).ToList();
        }

        /** Whether TaPoS values are required to resolve request. */
        public bool RequiresTapos()
        {
            var tx = GetRawTransaction();
            return !IsIdentity() && !SigningRequestConstants.HasTapos(tx);
        }

        /** Resolve required ABI definitions. */
        public async Task<Dictionary<string, Abi>> FetchAbis(IAbiSerializationProvider abiSerializationProvider)
        {
            var provider = abiSerializationProvider ?? _abiSerializationProvider;
            if (provider == null)
            {
                throw new Exception("Missing ABI provider");
            }

            var abis = new Dictionary<string, Abi>();

            foreach (var account in GetRequiredAbis())
            {
                abis.Add(account, await provider.GetAbi(account));
            }
            return abis;
        }

        /**
         * Decode raw actions actions to object representations.
         * @param abis ABI defenitions required to decode all actions.
         * @param signer Placeholders in actions will be resolved to signer if set.
         */
        public Action[] ResolveActions(Dictionary<string, Abi> abis, PermissionLevel signer)
        {
            return GetRawActions().Select(rawAction =>
            {
                Abi contractAbi = null; //: any | undefined
                if (SigningRequestConstants.IsIdentity(rawAction))
                {
                    contractAbi = SigningRequestAbi.Abi;
                    rawAction.account = "";

                    rawAction.data = new Dictionary<string, object>()
                    {
                        {"permission", new Dictionary<string, object>()
                            {
                                { "actor", signer.actor },
                                { "permission", signer.permission },
                            }
                        }
                    };
                    rawAction.authorization = new List<PermissionLevel>(){ signer };
                    rawAction.name = "identity";
                }
                else
                {
                    contractAbi = abis.SingleOrDefault(abi => abi.Key == rawAction.account).Value;
                }

                if (contractAbi == null)
                {
                    throw new Exception($"Missing ABI definition for {rawAction.account}");
                }

                if (signer != null)
                {
                    foreach (var auth in rawAction.authorization)
                    {
                        if ((auth.actor == SigningRequestConstants.PlaceholderName || auth.actor == null) && signer.actor != null)  
                        {
                            auth.actor = signer.actor;
                        }

                        if ((auth.permission == SigningRequestConstants.PlaceholderPermission || auth.permission == null) && signer.permission != null)
                        {
                            auth.permission = signer.permission;
                        }
                    }
                    
                    if (rawAction.data is Dictionary<string, object> dataDict)
                    {
                        ReplacePlaceholders(dataDict, signer);
                    }
                }

                var action = new Action()
                {
                    account = rawAction.account,
                    name = rawAction.name,
                    authorization = rawAction.authorization,
                    data = rawAction.data
                };

                if (signer != null)
                {
                    action.authorization = action.authorization.Select(auth =>
                    {
                        var actor = auth.actor;
                        var permission = auth.permission;
                        if (actor == SigningRequestConstants.PlaceholderName || actor == null)
                        {
                            actor = signer.actor;
                        }

                        if (permission == SigningRequestConstants.PlaceholderPermission || permission == null)
                        {
                            permission = signer.permission;
                        }

                        // backwards compatibility, actor placeholder will also resolve to permission when used in auth
                        if (permission == SigningRequestConstants.PlaceholderName)
                        {
                            permission = signer.permission;
                        }

                        return new PermissionLevel()
                        {
                            actor = actor,
                            permission = permission
                        };
                    }).ToList();
                }

                return action;
            }).ToArray();
        }

        private void ReplacePlaceholders(Dictionary<string, object> dataDict, PermissionLevel signer)
        {
            foreach (var dataDictKey in dataDict.Keys)
            {
                if (dataDict[dataDictKey] is string sVal)
                {
                    if (sVal == SigningRequestConstants.PlaceholderName && signer.actor != null)
                    {
                        dataDict[dataDictKey] = signer.actor;
                    }
                    else if (sVal == SigningRequestConstants.PlaceholderPermission && signer.permission != null)
                    {
                        dataDict[dataDictKey] = signer.permission;
                    }
                }
                else if (dataDict[dataDictKey] is PermissionLevel pVal && pVal == SigningRequestConstants.PlaceholderAuth && signer.permission != null && signer.actor != null)
                {
                    dataDict[dataDictKey] = signer;
                }
                else if(dataDict[dataDictKey] is Dictionary<string, object> innerDataDict)
                {
                    ReplacePlaceholders(innerDataDict, signer);
                }
            }
        }

        public Transaction ResolveTransaction(Dictionary<string, Abi> abis, PermissionLevel signer, TransactionContext ctx = null)
        {

            TransactionHeader SerializeTransactionHeader(TransactionContext ctx, uint expireSeconds)
            {
                uint prefix = 1;

                var transactionHeader = new TransactionHeader()
                {
                    Expiration = ctx.Timestamp.Value.AddSeconds(expireSeconds),
                    RefBlockNum = Convert.ToUInt16(ctx.RefBlockNum & 0xffff),
                    RefBlockPrefix = prefix // TODO
                };
                return transactionHeader;
            }

            var tx = GetRawTransaction();
            if (!IsIdentity() && !SigningRequestConstants.HasTapos(tx))
            {
                if (ctx.Expiration != null && ctx.RefBlockNum != null && ctx.RefBlockPrefix != null)
                {
                    tx.expiration = ctx.Expiration.Value;
                    tx.ref_block_num = ctx.RefBlockNum.Value;
                    tx.ref_block_prefix = ctx.RefBlockPrefix.Value;
                }
                else if (ctx.BlockNum != null && ctx.RefBlockPrefix != null && ctx.Timestamp != null)
                {
                    var header  = SerializeTransactionHeader(ctx, ctx.ExpireSeconds ?? 60);
                    tx.expiration = header.Expiration.Value;
                    tx.ref_block_num = ctx.BlockNum.Value;
                    tx.ref_block_prefix = ctx.RefBlockPrefix.Value;
                }
                else
                {
                    throw new Exception("Invalid transaction context, need either a reference block or explicit TAPoS values");
                }
            }

            var actions  = ResolveActions(abis, signer);
            tx.actions = actions.ToList();
            return tx;
        }

        public ResolvedSigningRequest Resolve(Dictionary<string, Abi> abis, PermissionLevel signer, TransactionContext ctx) {
            var transaction = ResolveTransaction(abis, signer, ctx);

            foreach (var action in transaction.actions)
            {
                if (action.account == "" && action.name == "identity")
                    if(!abis.ContainsKey(""))
                        abis.Add("", SigningRequestAbi.Abi);

                // Replace placeholder account and permission with signer-account and signer-permission
                if (action.data is byte[] actionBytes && !string.IsNullOrEmpty(action.name) && !string.IsNullOrEmpty(action.account))
                {
                    if(abis.TryGetValue(action.account, out var abi))
                    {
                        if (string.IsNullOrEmpty(action.hex_data))
                            action.hex_data = SerializationHelper.ByteArrayToHexString(actionBytes);                   

                        var deserializedData = _abiSerializationProvider.DeserializeStructData(action.name, action.hex_data, abi);
                        ReplacePlaceholders(deserializedData, signer);

                        action.data = deserializedData;
                    }
                }
            }

            var serializedTransaction = _abiSerializationProvider.SerializePackedTransaction(transaction, abis);
            return new ResolvedSigningRequest(this, signer, transaction, serializedTransaction);
        }
        
        /**
         * Get the id of the chain where this request is valid.
         * @returns The 32-byte chain id as hex encoded string.
         */
        public string GetChainId() {
            var id= Data.ChainId;
            switch (id.Key)
            {
                case "chain_id":
                    return (string)id.Value;
                case "chain_alias":
                    if (SigningRequestConstants.ChainIdLookup.ContainsKey((ChainName)id.Value))
                    {
                        return SigningRequestConstants.ChainIdLookup[(ChainName)id.Value];
                    }
                    else
                    {
                        throw new Exception("Unknown chain id alias");
                    }
                default:
                    throw new Exception("Invalid signing request data");
            }
        }

        /** Return the actions in this request with action data encoded. */
        public Action[] GetRawActions()
        {
            var req = Data.Req;
            switch (req.Key)
            {
                case "action":
                    if (req.Value is Action act)
                    {
                        return new[] { act };
                    }
                    else if (req.Value is Dictionary<string, object> dict)
                    {
                        var auths = new List<PermissionLevel>();
                        var authObj = dict["authorization"];
                        if (authObj is Dictionary<string, object> authDict)
                        {
                            auths.Add(new PermissionLevel()
                            {
                                actor = (string)authDict["actor"],
                                permission = (string)authDict["permission"]
                            });
                        }
                        else if (authObj is ICollection<object> authColl)
                        {
                            foreach (var authCollItem in authColl)
                            {
                                if (authCollItem is IDictionary<string, object> authCollItemDict)
                                {
                                    auths.Add(new PermissionLevel()
                                    {
                                        actor = (string)authCollItemDict["actor"],
                                        permission = (string)authCollItemDict["permission"]
                                    });
                                }
                            }
                        }

                        return new Action[]
                        {
                            new Action()
                            {
                                data = dict["data"],
                                authorization = auths,
                                name = (string)dict["name"],
                                account = (string)dict["account"]
                            }
                        };
                    }
                    else
                        throw new Exception("unsupported type for data.req");
                case "action[]":
                    return (Action[]) req.Value;
                case "identity":
                    var data = "0101000000000000000200000000000000"; // placeholder permission
                    var authorization = SigningRequestConstants.PlaceholderAuth;

                    if (req.Value is Dictionary<string, object> valueDict)
                    {
                        if (valueDict.TryGetValue("permission", out var permissionObj))
                        {
                            if (permissionObj != null)
                            {
                                data = SerializationHelper.ByteArrayToHexString(new byte[] { });
                            }
                        }
                    }

                    return new[]
                    {
                        new Action()
                        {
                            account = "",
                            name = "identity",
                            authorization = new List<PermissionLevel>(){ authorization },
                            hex_data = data
                        },
                    };
                case "transaction":
                    return ((Transaction)req.Value).actions.ToArray();
                default:
                    throw new Exception("Invalid signing request data");
            }
        }

        /** Unresolved transaction. */
        public Transaction GetRawTransaction() {
            var req  = Data.Req;
            switch (req.Key)
            {
                case "transaction":
                    return (Transaction)req.Value;
                case "action":
                case "action[]":
                case "identity":
                    return new Transaction()
                    {
                        actions = GetRawActions().ToList(),
                        context_free_actions = new List<Action>(),
                        transaction_extensions = new List<Extension>(),
                        expiration = new DateTime(1970, 1, 1),
                        ref_block_num = 0,
                        ref_block_prefix = 0,
                        max_cpu_usage_ms = 0,
                        max_net_usage_words = 0,
                        delay_sec = 0
                    };
                default:
                    throw new Exception("Invalid signing request data");
            }
        }

        /** Whether the request is an identity request. */
        public bool IsIdentity()
        {
            return Data.Req.Key == "identity";
        }

        /** Whether the request should be broadcast by signer. */
        public bool ShouldBroadcast() {
            if (IsIdentity())
            {
                return false;
            }

            return (Data.Flags & AbiConstants.RequestFlagsBroadcast) != 0;
        }

        /**
         * Present if the request is an identity request and requests a specific account.
         * @note This returns `nil` unless a specific identity has been requested,
         *       use `isIdentity` to check id requests.
         */
        public string GetIdentity() {
            if (Data.Req.Key == "identity")
            {
                var actor = SigningRequestConstants.PlaceholderName;
                if (Data.Req.Value is Dictionary<string, object> valueDict)
                {
                    if (valueDict.TryGetValue("permission", out var permission))
                    {
                        if (permission != null)
                        {
                            var permDict = (Dictionary<string, object>)permission;
                            if (permDict.TryGetValue("actor", out var actorObj))
                            {
                                actor = (string)actorObj;
                            }
                        }
                    }
                    return actor == SigningRequestConstants.PlaceholderName ? null : actor;
                }
            }
            return null;
        }

        /**
     * Present if the request is an identity request and requests a specific permission.
     * @note This returns `nil` unless a specific permission has been requested,
     *       use `isIdentity` to check id requests.
     */
        public string GetIdentityPermission() {
            if (Data.Req.Key == "identity")
            {
                var permission = SigningRequestConstants.PlaceholderPermission;
                if (Data.Req.Value is Dictionary<string, object> valueDict)
                {
                    if (valueDict.TryGetValue("permission", out var permissionObj))
                    {
                        if (permissionObj != null)
                        {
                            var permDict = (Dictionary<string, object>)permissionObj;
                            if (permDict.TryGetValue("permission", out var permObj))
                            {
                                permission = (string)permObj;
                            }
                        }
                    }
                    return permission == SigningRequestConstants.PlaceholderPermission ? null : permission;
                }
            }
            return null;
        }

        /** Get raw info dict */
        public Dictionary<string, byte[]> GetRawInfo()
        {
            var rv = new Dictionary<string, byte[]>();

            foreach (var info in Data.Info)
            {
                if(info is InfoPair infoPair )
                    rv.Add(infoPair.Key, infoPair.Value is string ? SerializationHelper.HexStringToByteArray((string)infoPair.Value) : (byte[])infoPair.Value);
                else if(info is Dictionary<string, object> infoDict)
                {
                    if(infoDict.ContainsKey("key") && infoDict.ContainsKey("value"))
                        rv.Add((string)infoDict["key"], infoDict["value"] is string ? SerializationHelper.HexStringToByteArray((string)infoDict["value"]) : (byte[])infoDict["value"]);                         
                    else
                    {
                        throw new NotSupportedException("Dictionary without \"key\" and \"value\" not supported");
                    }
                }
            }
            return rv;
        }

        /** Get metadata values as T */
        public T GetInfo<T>(string key, string abiSerializableType)
        {
            var rv = new Dictionary<string, string>();
            var raw = GetRawInfo();
            if (raw.TryGetValue(key, out var rawVal))
            {
                return _abiSerializationProvider.Deserialize<T>(rawVal, abiSerializableType);
            }
            else
                throw new KeyNotFoundException($"Key {key} not found");
        }

        /** Get metadata values as T */
        public string GetInfo(string key, string abiSerializableType = "string")
        {
            var raw = GetRawInfo();
            if (raw.TryGetValue(key, out var rawVal))
            {
                return _abiSerializationProvider.Deserialize<string>(rawVal, abiSerializableType);
            }
            else
                throw new KeyNotFoundException($"Key {key} not found");
        }

        /** Get metadata values as strings. */
        public Dictionary<string, string> GetInfos()
        {
            var rv = new Dictionary<string, string>();
            var raw = GetRawInfo();

            foreach (var rawInfo in raw)
            {
                rv.Add(rawInfo.Key, _abiSerializationProvider.Deserialize<string>(rawInfo.Value, "string"));
            }

            return rv;
        }

        /** Set a metadata key. */
        public void SetInfoKey(string key, object value, string abiSerializableType = null)
        {
            var infoPairs = Data.Info.Cast<InfoPair>().ToList();
            var pair = infoPairs.SingleOrDefault(i => i.Key == key); 
            
            byte[] encodedValue;
            if (abiSerializableType == null)
            {
                switch (value)
                {
                    case string stringtype:
                        encodedValue = Encoding.UTF8.GetBytes(stringtype);
                        break;
                    case bool booltype:
                        encodedValue = new byte[] { Convert.ToByte(booltype ? 1 : 0) };
                        break;
                    default:
                        throw new Exception("Invalid value type, expected string or boolean.");
                }
                if (pair != null)
                    infoPairs.Remove(pair);

                pair = new InfoPair()
                {
                    Key = key,
                    Value = encodedValue
                };
                Data.Info.Add(pair);
            }
            else
            {
                encodedValue = _abiSerializationProvider.Serialize(value, abiSerializableType);
                pair = new InfoPair()
                {
                    Key = key,
                    Value = encodedValue
                };
                Data.Info.Add(pair);
            }

        }

        /** Return a deep copy of this request. */
        public SigningRequest Clone()
        {
            SigningRequestData clonedData = null;
            if(Data != null)
                clonedData = new SigningRequestData()
                {
                    Req = Data.Req,
                    Callback = Data.Callback,
                    Info = Data.Info,
                    ChainId = Data.ChainId,
                    Flags = Data.Flags
                };
            RequestSignature clonedRequestSignature = null;
            if (Signature != null)
                clonedRequestSignature = new RequestSignature()
                {
                    Signature = Signature.Signature,
                    Signer = Signature.Signer
                };

            return new SigningRequest(
                Version,
                clonedData,
                _zlib,
                _abiSerializationProvider,
                clonedRequestSignature
            );
        }
        
        /**
         * Present if the request is an identity request and requests a specific permission.
         * @note This returns `nil` unless a specific permission has been requested,
         *       use `isIdentity` to check id requests.
         */
        public string GetIdentityScope() {
            if (!IsIdentity() || Version <= 2)
            {
                return null;
            }

            var id = Data.Req.Value as IdentityV3;
            return id.Scope;
        }
}
}