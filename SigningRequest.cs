/**
 * EOSIO Signing Request (ESR).
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Helpers;
using EosSharp.Core.Interfaces;
using EosSharp.Core.Providers;

using CallbackType = System.Object; // TODO export type CallbackType = string | {url: string; background: boolean}*/
using AbiMap = System.Collections.Generic.Dictionary<string, EosSharp.Core.Api.v1.Abi>; //     export type AbiMap = Map<string, any>
using RequestFlags = System.Byte;  //number;  // TODO
using ChainId = System.String; /*checksum256*/
using VariantId = System.Collections.Generic.KeyValuePair<string, object>;
using Newtonsoft.Json;
using Action = EosSharp.Core.Api.v1.Action;

namespace EosioSigningRequest
{
    public static class Constants
    {
        public static Dictionary<ChainName, string> ChainIdLookup = new Dictionary<ChainName, string>() {
            {ChainName.EOS, "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906"},
            {ChainName.TELOS, "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11"},
            {ChainName.JUNGLE, "e70aaab8997e1dfce58fbfac80cbbb8fecec7b99cf982a9444273cbc64c41473"},
            {ChainName.KYLIN, "5fff1dae8dc8e2fc4d5b23b2c7665c97f9e9d8edf2b6485a86ba311c25639191"},
            {ChainName.WORBLI, "73647cde120091e0a4b85bced2f3cfdb3041e266cbbe95cee59b73235a1b3b6f"},
            {ChainName.BOS, "d5a3d18fbb3c084e3b1f3fa98c21014b5f3db536cc15d08f9f6479517c6a3d86"},
            {ChainName.MEETONE, "cfe6486a83bad4962f232d48003b1824ab5665c36778141034d75e57b956e422"},
            {ChainName.INSIGHTS, "b042025541e25a472bffde2d62edd457b7e70cee943412b1ea0f044f88591664"},
            {ChainName.BEOS, "b912d19a6abd2b1b05611ae5be473355d64d95aeff0c09bedc8c166cd6468fe4"},
            {ChainName.WAX, "1064487b3cd1a897ce03ae5b6a865651747e2e152090f99c1d19d44e01aea5a4"},
            {ChainName.PROTON, "384da888112027f0321850a169f737c33e53b388aad48b5adace4bab97f437e0"},
            {ChainName.FIO, "21dcae42c0182200e93f954a074011f9048a7624c6fe81d3c9541a614a88bd1c"} 
        };

        public static string PlaceholderName = "............1";

        public static string PlaceholderPermission = "............2";

        public static byte ProtocolVersion = 2;

        public static EosSharp.Core.Api.v1.PermissionLevel PlaceholderAuth = new EosSharp.Core.Api.v1.PermissionLevel()
        {
            actor = PlaceholderName,
            permission = PlaceholderPermission
        };

        public static bool isIdentity(EosSharp.Core.Api.v1.Action action)
        {
            return action.account == "" && action.name == "identity";
        }

        public static bool hasTapos(EosSharp.Core.Api.v1.Transaction tx)
        {
            return !(tx.expiration == new DateTime(1970, 1, 1) && tx.ref_block_num == 0 && tx.ref_block_prefix == 0);
        }

        public static VariantId variantId(object chainId /*abi.ChainId | abi.ChainAlias*/){
            if (chainId == null)
            {
                chainId = (byte)ChainName.EOS;
            }
            if (chainId is byte)
            {
                return new VariantId("chain_alias", chainId);
            }
            if (chainId is string chainIdString && Constants.ChainIdLookup.ContainsValue(chainIdString))
            {
                return new VariantId("chain_alias",
                    (byte)Constants.ChainIdLookup.FirstOrDefault(c => c.Value == chainIdString).Key);
            }
            if(chainId is string chainIdShortString && Enum.TryParse(chainIdShortString, out ChainName chainIdEnum))
            {
                // resolve known chain id's to their aliases
                if (chainIdEnum != ChainName.UNKNOWN)
                    return new VariantId("chain_alias", (byte) chainIdEnum);
                return new VariantId("chain_id", Constants.ChainIdLookup[(ChainName)chainIdEnum]);
            }
            return new VariantId("chain_alias", chainId);
        }

        public static string nameToId(long id)
        {
            // TODO
            return "";
        }
    }

    /** Interface that should be implemented by zlib implementations. */
    public interface IZlibProvider
    {
        /** Deflate data w/o adding zlib header. */
        byte[] deflateRaw(byte[] data);

        /** Inflate data w/o requiring zlib header. */
        byte[] inflateRaw(byte[] data);
    }

    /** Interface that should be implemented by signature providers. */
    //public interface ISignatureProvider
    //{
    //    /** Sign 32-byte hex-encoded message and return signer name and signature string. */
    //    RequestSignature sign(string message);// => {signer: string; signature: string}  // TODO
    //}

    /**
     * The callback payload sent to background callbacks.
     */
    public class CallbackPayload
    {
        /** The first signature. */
        public string sig;

        /** Transaction ID as HEX-encoded string. */
        public string tx;

        /** Block number hint (only present if transaction was broadcast). */
        public string bn;

        /** Signer authority, aka account name. */
        public string sa;

        /** Signer permission, e.g. "active". */
        public string sp;

        /** Reference block num used when resolving request. */
        public string rbn;

        /** Reference block id used when resolving request. */
        public string rid;

        /** The originating signing request packed as a uri string. */
        public string req;

        /** Expiration time used when resolving request. */
        public string ex;

        /** All signatures 0-indexed as `sig0`, `sig1`, etc. */
        public Dictionary<string, string> sigs; // TODO
        //    [sig0: string]: string | undefined    // TODO
        public Dictionary<string, string> data;
    }

    /**
     * Context used to resolve a callback.
     * Compatible with the JSON response from a `push_transaction` call.
     */
    public class ResolvedCallback
    {
        /** The URL to hit. */
        public string url;

        /**
         * Whether to run the request in the background. For a https url this
         * means POST in the background instead of a GET redirect.
         */
        public bool background;

        /**
         * The callback payload as a object that should be encoded to JSON
         * and POSTed to background callbacks.
         */
        public CallbackPayload payload;
    }

    /**
     * Context used to resolve a transaction.
     * Compatible with the JSON response from a `get_block` call.
     */
    public class TransactionContext
    {
        /** Timestamp expiration will be derived from. */
        public DateTime? timestamp;

        /**
         * How many seconds in the future to set expiration when deriving from timestamp.
         * Defaults to 60 seconds if unset.
         */
        public uint? expire_seconds;

        /** Block number ref_block_num will be derived from. */
        public ushort? block_num;

        /** Reference block number, takes precedence over block_num if both is set. */
        public ushort? ref_block_num;

        /** Reference block prefix. */
        public uint? ref_block_prefix;

        /** Expiration timestamp, takes precedence over timestamp and expire_seconds if set. */
        public DateTime? expiration;
    }

    /** Chain ID aliases. */
    public enum ChainName : byte
    {
        UNKNOWN = 0, // reserved
        EOS = 1,
        TELOS = 2,
        JUNGLE = 3,
        KYLIN = 4,
        WORBLI = 5,
        BOS = 6,
        MEETONE = 7,
        INSIGHTS = 8,
        BEOS = 9,
        WAX = 10,
        PROTON = 11,
        FIO = 12,
    }

    /**
 * The placeholder name: `............1` aka `uint64(1)`.
 * If used in action data will be resolved to current signer.
 * If used in as an authorization permission will be resolved to
 * the signers permission level.
 *
 * Example action:
 * ```
 * { account: "eosio.token",
 *   name: "transfer",
 *   authorization: [{actor: "............1", permission: "............1"}],
 *   data: {
 *     from: "............1",
 *     to: "bar",
 *     quantity: "42.0000 EOS",
 *     memo: "Don't panic" }}
 * ```
 * When signed by `foo@active` would resolve to:
 * ```
 * { account: "eosio.token",
 *   name: "transfer",
 *   authorization: [{actor: "foo", permission: "active"}],
 *   data: {
 *     from: "foo",
 *     to: "bar",
 *     quantity: "42.0000 EOS",
 *     memo: "Don't panic" }}
 * ```
 */
// TODO
/*export const PlaceholderName = '............1' // aka uint64(1)

/** Placeholder that will resolve to signer permission name. */
/*export const PlaceholderPermission = '............2' // aka uint64(2)

export const PlaceholderAuth: abi.PermissionLevel = {
    actor: PlaceholderName,
    permission: PlaceholderPermission,
}*/

    public class SigningRequestCreateArguments
    {

        /** Single action to create request with. */
        public EosSharp.Core.Api.v1.Action action;
        //public Action action;

        /** Multiple actions to create request with. */
        public EosSharp.Core.Api.v1.Action[] actions;

        /**
         * Full or partial transaction to create request with.
         * If TAPoS info is omitted it will be filled in when resolving the request.
         */
        public EosSharp.Core.Api.v1.Transaction transaction;

        /** Create an identity request. */
        public IdentityV2 Identity;

        /** Chain to use, defaults to EOS main-net if omitted. */
        public string chainId;

        /** Whether wallet should broadcast tx, defaults to true. */
        public bool? broadcast;

        /**
        * Optional callback URL the signer should hit after
        * broadcasting or signing. Passing a string means background = false.
        */
        public CallbackType callback;

        /** Optional metadata to pass along with the request. */
        public object info; // TODO: {[key: string]: string | Uint8Array}   // TODO
        // Dictionary or string ?
    }

    public class SigningRequestCreateIdentityArguments
    {
        /**
     * Callback where the identity should be delivered.
     */
        public CallbackType callback;

        /** Chain to use, defaults to EOS if omitted. */
        public string chainId;

        /**
         * Requested account name of identity.
         * Defaults to placeholder (any identity) if omitted.
         */
        public string account;

        /**
         * Requested account permission.
         * Defaults to placeholder (any permission) if omitted.
         */
        public string permission;

        /** Optional metadata to pass along with the request. */
        public object info; // TODO ?: {[key: string]: string | Uint8Array}   // TODO
    }

    public class SigningRequestEncodingOptions
    {
        /** UTF-8 text encoder, required when using node.js. */
        //textEncoder?: any
        /** UTF-8 text decoder, required when using node.js. */
        //textDecoder?: any
        
        /** Optional zlib, if provided the request will be compressed when encoding. */
        public IZlibProvider zlib;

        /** Abi provider, required if the arguments contain un-encoded actions. */
        public IAbiSerializationProvider abiSerializationProvider;
        
        /** Optional signature provider, will be used to create a request signature if provided. */
        public ISignProvider signatureProvider;
    }

    public partial class SigningRequest
    {
        public static AbiStruct type = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "signing_request");
        public static AbiStruct type_v3 = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "signing_request_identity_v3");
        public static AbiStruct idType = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "identity");
        public static AbiStruct transactionType = SigningRequestAbi.Abi.structs.FirstOrDefault(s => s.name == "transaction");

        /** Create a new signing request. */
        public static async Task<SigningRequest> create(SigningRequestCreateArguments args, SigningRequestEncodingOptions options)
        {
            byte version = 2;

            async Task<EosSharp.Core.Api.v1.Action> serialize(EosSharp.Core.Api.v1.Action action)
            {
                if (string.IsNullOrEmpty(action.hex_data))
                {
                    var abi = await options.abiSerializationProvider.GetAbi(action.account);
                    action.hex_data =
                        SerializationHelper.ByteArrayToHexString(
                            options.abiSerializationProvider
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
                    data.req = new VariantId("identity_v3", args.Identity);
                }
                else
                {
                    data.req = new VariantId("identity", args.Identity);
                }
            }
            else if (args.action != null && args.actions == null && args.transaction == null)
            {
                data.req = new VariantId("action", await serialize(args.action));
            }
            else if (args.actions != null && args.action == null && args.transaction == null)
            {
                if (args.actions.Length == 1)
                {
                    data.req = new VariantId("action", await serialize(args.actions[0]));
                }
                else
                {
                    data.req = new VariantId("action[]", args.actions.Select(async action => await serialize(action)).Select(t => t.Result).ToArray());
                }
            }
            else if (args.transaction != null && args.action == null && args.actions == null)
            {
                var tx = args.transaction;
                // set default values if missing

                if (tx.context_free_actions == null)
                {
                    tx.context_free_actions = new List<EosSharp.Core.Api.v1.Action>();
                }

                if (tx.transaction_extensions == null)
                {
                    tx.transaction_extensions = new List<EosSharp.Core.Api.v1.Extension>();
                }

                // encode actions if needed
                tx.actions = tx.actions.Select(async action => await serialize(action)).Select(t => t.Result).ToList();
                data.req = new VariantId("transaction", tx);  // TODO !
            }
            else
            {
                throw new Exception("Invalid arguments: Must have exactly one of action, actions or transaction");
            }

            // set the chain id
            data.chain_id = Constants.variantId(args.chainId);
            data.flags = AbiConstants.RequestFlagsNone;

            bool broadcast = args.broadcast ?? data.req.Key != "identity" && data.req.Key != "identity_v3";
            if (broadcast)
            {
                data.flags |= AbiConstants.RequestFlagsBroadcast;
            }

            if (args.callback is string callback)
            {
                data.callback = callback;
            }
            else if(args.callback is Dictionary<string, object> callbackDict)
            {
                if (callbackDict.TryGetValue("url", out var url) && url is string stringUrl)
                    data.callback = stringUrl;
                if(callbackDict.TryGetValue("background",out var brdcst) && brdcst is bool brdcstBool)
                    data.flags |= AbiConstants.RequestFlagsBackground;
            }
            else if (args.callback is KeyValuePair<string, bool> obj)
            {
                data.callback = obj.Key;
                if (obj.Value)
                {
                    data.flags |= AbiConstants.RequestFlagsBackground;
                }
            }
            else {
                data.callback = "";
            }

            data.info = new List<object>();
            if (args.info != null && args.info is List<InfoPair> infoPairs)
                foreach (var infoPair in infoPairs)
                {
                    if (infoPair.value is string stringVal)
                        data.info.Add(new InfoPair(infoPair.key, options.abiSerializationProvider.Serialize(stringVal, "string")));
                    else
                        data.info.Add(infoPair);
                }
            if (args.info is Dictionary<string, string> dictionary) {
                foreach (var info in dictionary)
                {
                    data.info.Add(new InfoPair()
                    {
                        key = info.Key,
                        value = info.Value
                    });
                }
            }

            var req = new SigningRequest( 
                version,
                data,
                options.zlib,
                options.abiSerializationProvider,
                null
            );

            // sign the request if given a signature provider
            if (options.signatureProvider != null)
            {
                req.sign(options.signatureProvider);
            }

            return req;
        }

        /** Creates an identity request. */
        public static SigningRequest identity(SigningRequestCreateIdentityArguments args, SigningRequestEncodingOptions options)
        {
            EosSharp.Core.Api.v1.PermissionLevel permission = new EosSharp.Core.Api.v1.PermissionLevel()
            {
                actor = args.account ?? Constants.PlaceholderName,
                permission = args.permission ?? Constants.PlaceholderPermission
            };

            if (permission.actor == Constants.PlaceholderName && permission.permission == Constants.PlaceholderPermission)
            {
//                permission = null;
            }
            return create(new SigningRequestCreateArguments()
            {
                Identity = new IdentityV2(){ permission = permission },
                broadcast = false,
                callback = args.callback,
                info = args.info
            }, options).Result; // TODO async await + method async?
        }

        /**
         * Create a request from a chain id and serialized transaction.
         * @param chainId The chain id where the transaction is valid.
         * @param serializedTransaction The serialized transaction.
         * @param options Creation options.
         */
        public static SigningRequest fromTransaction(object chainId /*Uint8Array | string*/, 
            object serializedTransaction /*Uint8Array | string*/, 
            SigningRequestEncodingOptions options) 
        {
            if (chainId is byte[] byteId)
            {
                chainId = SerializationHelper.ByteArrayToHexString(byteId);
            }
            if (serializedTransaction is string transaction)
            {
                serializedTransaction = SerializationHelper.HexStringToByteArray(transaction);
            }

            using (MemoryStream buf = new MemoryStream())
            {
                buf.WriteByte(2); // header
                var id= Constants.variantId(chainId);
                if (id.Key == "chain_alias")
                {
                    buf.WriteByte(0);
                    buf.WriteByte(Convert.ToByte((int)id.Value));
                }
                else
                {
                    buf.WriteByte(1);
                    byte[] bytes = SerializationHelper.HexStringToByteArray((string)id.Value);
                    buf.Write(bytes, 0, bytes.Length);
                }

                buf.WriteByte(2); // transaction variant
                buf.Write((byte[])serializedTransaction,0, ((byte[])serializedTransaction).Length);
                buf.WriteByte(AbiConstants.RequestFlagsBroadcast); // flags
                buf.WriteByte(0); // callback
                buf.WriteByte(0); // info

                return fromData(buf.ToArray(), options);
            }
        }

        /** Creates a signing request from encoded `esr:` uri string. */
        public static SigningRequest from(string uri, SigningRequestEncodingOptions options) {
            //const [scheme, path] = uri.split(':')
            string[] subs = uri.Split(':');
            string scheme = subs[0];
            string path = subs[1];
            if (scheme != "esr" && scheme != "web+esr")
            {
                throw new Exception("Invalid scheme");
            }

            path = path.StartsWith("//") ? path.Substring(2) : path;
            //if (path.Length % 4 != 0 && !path.EndsWith(padding.ToString()))
            //    path += padding;
            //path = path.Replace('-', '+').Replace('_', '/');
            
            Console.WriteLine(path);
            byte[] data = Base64EncodingUtility.FromBase64UrlSafe(path);
//            byte[] data = Convert.FromBase64String(path);
            return fromData(data, options);
        }

        public static SigningRequest fromData(byte[] data, SigningRequestEncodingOptions options ) {
            byte header  = data[0];
            byte version = (byte)(header & ~(1 << 7));
            //if (version != Constants.ProtocolVersion)
            //{
            //    throw new Exception("Unsupported protocol version");
            //}

            byte[] array = new byte[data.Length-1];
            Array.Copy(data, 1, array, 0, data.Length-1);
            
            if ((header & (1 << 7)) != 0)
            {
                if (options.zlib == null)
                {
                    throw new Exception("Compressed URI needs zlib");
                }

                array = options.zlib.inflateRaw(array);
            }


            int readIndex = 0;
            SigningRequestData requestData = null;
            if(version == 3)
                requestData = options.abiSerializationProvider.DeserializeStructData<SigningRequestData>("signing_request_identity_v3", array, SigningRequestAbi.Abi, ref readIndex);
            else
                requestData = options.abiSerializationProvider.DeserializeStructData<SigningRequestData>("signing_request", array, SigningRequestAbi.Abi, ref readIndex);

            if (requestData.req.Key == "action" && requestData.req.Value is Dictionary<string, object> valueDict)
            {
                if (valueDict.ContainsKey("data") && valueDict["data"] is byte[] bytes)
                {
                    valueDict.Add("hex_data", SerializationHelper.ByteArrayToHexString(bytes));
                }
            }

            RequestSignature signature = null;
            if (readIndex < array.Length)
            {
                signature = options.abiSerializationProvider.DeserializeStructData<RequestSignature>("request_signature", array, SigningRequestAbi.Abi, ref readIndex);
            }

            /*            var req = type.deserialize(buffer); // array to buffer
                        var signature = new RequestSignature();

                        if (buffer.haveReadData())
                        {
                            const type = AbiTypes.get("request_signature")!;
                            signature = type.deserialize(buffer);
                        }*/

            return new SigningRequest(
                version,
                requestData,
                options.zlib,
                options.abiSerializationProvider,
                signature
            );
        }

        /** The signing request version. */
        public byte version = 2;

        /** The raw signing request data. */
        public SigningRequestData data;

        /** The request signature. */
        public RequestSignature signature;

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
            if ((data.flags & AbiConstants.RequestFlagsBroadcast) != 0 && (data.req.Key == "identity" || data.req.Key == "identity_v3"))
            {
                throw new Exception("Invalid request (identity request cannot be broadcast)");
            }

            if ((data.flags & AbiConstants.RequestFlagsBroadcast) == 0 && data.callback.Length == 0)
            {
                throw new Exception("Invalid request (nothing to do, no broadcast or callback set)");
            }

            this.version = version;
            this.data = data;
            this._zlib = zlib;
            this._abiSerializationProvider = abiSerializationProvider;
            this.signature = signature;
        }

        /**
         * Sign the request, mutating.
         * @param signatureProvider The signature provider that provides a signature for the signer.
         */
        public void sign(ISignProvider signatureProvider)
        {
            byte[] message = getSignatureDigest();
            var signatureData = signatureProvider.Sign(getChainId(), message);
            signature = new RequestSignature()
            {
                signer = Constants.PlaceholderName,
                signature = signatureData
            };
        }

        /**
         * Get the signature digest for this request.
         */
        public byte[] getSignatureDigest()
        {

            // TODO, is the following correct?

            // protocol version + utf8 "request"
            byte[] versionUtf8 = {this.version, 0x72, 0x65, 0x71, 0x75, 0x65, 0x73, 0x74};
            byte[] data = getData();

            byte[] req = new byte[versionUtf8.Length + data.Length];
            versionUtf8.CopyTo(req, 0);
            data.CopyTo(req, versionUtf8.Length);
            return Sha256Manager.GetHash(req);
        }

        /**
         * Set the signature data for this request, mutating.
         * @param signer Account name of signer.
         * @param signature The signature string.
         */
        public void setSignature(string signer, string signature)
        {
            this.signature = new RequestSignature()
            {
                signer = signer,
                signature = signature
            };
        }

        /**
         * Set the request callback, mutating.
         * @param url Where the callback should be sent.
         * @param background Whether the callback should be sent in the background.
         */
        public void setCallback(string url, bool background)
        {
            this.data.callback = url;
            if (background)
            {
                this.data.flags |= AbiConstants.RequestFlagsBackground;
            }
            else
            {
                this.data.flags = ((byte)(this.data.flags &  ~AbiConstants.RequestFlagsBackground));
            }
        }

        /**
         * Set broadcast flag.
         * @param broadcast Whether the transaction should be broadcast by receiver.
         */
        public void setBroadcast(bool broadcast)
        {
            if (broadcast)
            {
                this.data.flags |= AbiConstants.RequestFlagsBroadcast;
            }
            else
            {
                this.data.flags = ((byte)(this.data.flags & ~AbiConstants.RequestFlagsBroadcast));
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
        static readonly char padding =  '=';
        public string encode(bool? compress = null, bool? slashes = null)
        {
            bool shouldCompress = compress ?? this._zlib != null;
            if (shouldCompress && this._zlib == null)
            {
                throw new Exception("Need zlib to compress");
            }

            var header = this.version;
            byte[] data = getData();
            var data2 = new byte[]
            {
                131,99,96,100,102,248,240,31,2,152,132,51,74,74,10,138,173,244,245,83,43,18,115,11,114,82,245,146,243,115,25,0
            };
            byte[] sigData = getSignatureData();
            byte[] array = new byte[data.Length + sigData.Length];
            data.CopyTo(array,0);
            sigData.CopyTo(array, data.Length);
            if (shouldCompress)
            {
                var deflated = _zlib!.deflateRaw(array);
                if (array.Length > deflated.Length)
                {
                    header |= 1 << 7;
                    array = deflated;
                }
            }

            byte[] output = new byte[1 + array.Length];
            output[0] = header;
            array.CopyTo(output, 1);
            string scheme = "esr:";
            if (slashes != false)
            {
                scheme += "//";
            }

            return scheme + Base64EncodingUtility.ToBase64UrlSafe(output); //Convert.ToBase64String(output).TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }

        /** Get the request data without header or signature. */
        public byte[] getData()
        {
            if (data.req.Key == "action")
            {
                if (data.req.Value is Action action)
                {
                    ((Action)data.req.Value).data = SerializationHelper.HexStringToByteArray(action.hex_data);
                }
                else if (data.req.Value is Dictionary<string, object> dict)
                {
                    var auth = (List<object>)dict["authorization"];

                    var authorization = new List<PermissionLevel>();
                    foreach (Dictionary<string, object> permLevel in auth)
                    {
                        authorization.Add(new PermissionLevel()
                            { actor = (string)permLevel["actor"], permission = (string)permLevel["permission"] });
                    }

                    data.req = new KeyValuePair<string, object>(data.req.Key,
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
                    throw new Exception($"type {data.req.Value.GetType().Name} not supported");
                }
            }
            else if (data.req.Key == "action[]")
            {
                throw new NotImplementedException();
            }
            else if (data.req.Key == "identity" || data.req.Key == "identity_v3")
            {

            }
            else
            {
                throw new NotSupportedException($"request key \"{data.req.Key}\" not supported");
            }
            

            if(version == 2)
                return _abiSerializationProvider.SerializeStructData(data, type, SigningRequestAbi.Abi);
            if(version == 3)
                return _abiSerializationProvider.SerializeStructData(data, type_v3, SigningRequestAbi.Abi);
            return Array.Empty<byte>();
        }

        /** Get signature data, returns an empty array if request is not signed. */
        public byte[] getSignatureData() {
            if (signature == null)
            {
                return Array.Empty<byte>();
            }

            var structType = SigningRequestAbi.Abi.structs.FirstOrDefault(t => t.name == "request_signature")!;
            return _abiSerializationProvider.SerializeStructData(signature, structType, SigningRequestAbi.Abi);
        }

        /** ABI definitions required to resolve request. */
        public List<string> getRequiredAbis()
        {
            return getRawActions().Where(a => !Constants.isIdentity(a)).Select(a => a.account).ToList();

/*            return this.getRawActions()
                .filter((action) => !Constants.isIdentity(action))
                .map((action) => action.account)
                .filter((value, index, self) => self.indexOf(value) == index)*/
        }

        /** Whether TaPoS values are required to resolve request. */
        public bool requiresTapos()
        {
            var tx = getRawTransaction();
            return !isIdentity() && !Constants.hasTapos(tx);
        }

        /** Resolve required ABI definitions. */
        public async Task<AbiMap> fetchAbis(IAbiSerializationProvider abiSerializationProvider)
        {
            var provider = abiSerializationProvider ?? this._abiSerializationProvider;
            if (provider == null)
            {
                throw new Exception("Missing ABI provider");
            }

            var abis = new Dictionary<string, Abi>();    // TODO, how does Scatter do this?

            foreach (var account in getRequiredAbis())
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
        public EosSharp.Core.Api.v1.Action[] resolveActions(AbiMap abis, EosSharp.Core.Api.v1.PermissionLevel signer)
        {
            return getRawActions().Select(rawAction =>
            {
//                Abi abi;
//                if (Constants.isIdentity(rawAction))
//                {
//                    abi = SigningRequestAbi.Abi;//(this.constructor as typeof SigningRequest).identityAbi(this.version)
//                }
//                else
//                {
//                    if (!abis.ContainsKey(rawAction.account))
//                    {
//                        throw new Exception($"Missing ABI definition for {rawAction.account}");
//                    }
//                    abi = abis[rawAction.account];
////                    abi = ABI.from(rawAbi)
//                }
//                if (abi.actions.All(a => a.name != rawAction.name)) {
//                    throw new Exception($"Missing type for action ${ rawAction.account}:{rawAction.name} in ABI"")
//                }

//                var type = abi.actions.FirstOrDefault(a => a.name == rawAction.name);

                Abi contractAbi = null; //: any | undefined
                if (Constants.isIdentity(rawAction))
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


                    //rawAction.data = new Dictionary<string, object>()
                    //{
                    //    { "actor", signer.actor },
                    //    { "permission", signer.permission },
                    //};
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
                        if ((auth.actor == Constants.PlaceholderName || auth.actor == null) && signer.actor != null)  
                        {
                            auth.actor = signer.actor;
                        }

                        if ((auth.permission == Constants.PlaceholderPermission || auth.permission == null) && signer.permission != null)
                        {
                            auth.permission = signer.permission;
                        }
                    }
                    
                    if (rawAction.data is Dictionary<string, object> dataDict)
                    {
                        ReplacePlaceholders(dataDict, signer);
                    }
                }

                EosSharp.Core.Api.v1.Action action = new Action()
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
                        string actor = auth.actor;
                        string permission = auth.permission;
                        if (actor == Constants.PlaceholderName || actor == null)
                        {
                            actor = signer.actor;
                        }

                        if (permission == Constants.PlaceholderPermission || permission == null)
                        {
                            permission = signer.permission;
                        }

                        // backwards compatibility, actor placeholder will also resolve to permission when used in auth
                        if (permission == Constants.PlaceholderName)
                        {
                            permission = signer.permission;
                        }

                        return new EosSharp.Core.Api.v1.PermissionLevel()
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
                    if (sVal == Constants.PlaceholderName && signer.actor != null)
                    {
                        dataDict[dataDictKey] = signer.actor;
                    }
                    else if (sVal == Constants.PlaceholderPermission && signer.permission != null)
                    {
                        dataDict[dataDictKey] = signer.permission;
                    }
                }
                else if (dataDict[dataDictKey] is PermissionLevel pVal && pVal == Constants.PlaceholderAuth && signer.permission != null && signer.actor != null)
                {
                    dataDict[dataDictKey] = signer;
                }
                else if(dataDict[dataDictKey] is Dictionary<string, object> innerDataDict)
                {
                    ReplacePlaceholders(innerDataDict, signer);
                }
            }
        }

        public EosSharp.Core.Api.v1.Transaction resolveTransaction(AbiMap abis, EosSharp.Core.Api.v1.PermissionLevel signer, TransactionContext ctx = null/*TODO null?*/)
        {

            TransactionHeader serializeTransactionHeader(TransactionContext ctx, uint expire_seconds)
            {

                uint prefix = 1;//SerializationHelper.ReverseHex(ctx.) -- parseInt(reverseHex(refBlock.id.substr(16, 8)), 16); // TODO

                TransactionHeader transactionHeader = new TransactionHeader()
                {
                    expiration = ctx.timestamp.Value.AddSeconds(expire_seconds),
                    ref_block_num = Convert.ToUInt16(ctx.ref_block_num & 0xffff),
                    ref_block_prefix = prefix // TODO
                };
                return transactionHeader;
            }

            var tx = getRawTransaction();
            if (!isIdentity() && !Constants.hasTapos(tx))
            {
                if (ctx.expiration != null && ctx.ref_block_num != null && ctx.ref_block_prefix != null)
                {
                    tx.expiration = ctx.expiration.Value;// TODO !!!
                    tx.ref_block_num = ctx.ref_block_num.Value;
                    tx.ref_block_prefix = ctx.ref_block_prefix.Value;
                }
                else if (ctx.block_num != null && ctx.ref_block_prefix != null && ctx.timestamp != null)
                {
                    var header  = serializeTransactionHeader(ctx, ctx.expire_seconds ?? 60);
                    tx.expiration = header.expiration.Value;
                    tx.ref_block_num = ctx.block_num.Value;//Convert.ToUInt16(header.ref_block_num.Value);
                    tx.ref_block_prefix = ctx.ref_block_prefix.Value;//header.ref_block_prefix.Value;
                }
                else
                {
                    throw new Exception("Invalid transaction context, need either a reference block or explicit TAPoS values");
                }
            }

            var actions  = this.resolveActions(abis, signer);
            tx.actions = actions.ToList();
            return tx;
            //return new EosSharp.Core.Api.v1.Transaction()
            //{
            //    actions = actions.ToList(),
            //    context_free_actions = new List<Action>(), // TODO ?,
            //    delay_sec = tx.delay_sec, // TODO ?
            //    expiration = tx.expiration,
            //    max_cpu_usage_ms = tx.max_cpu_usage_ms, // TODO ?
            //    max_net_usage_words = tx.max_net_usage_words, // TODO ?
            //    ref_block_num = tx.ref_block_num,
            //    ref_block_prefix = tx.ref_block_prefix,
            //    transaction_extensions = new List<Extension>() // TODO ?
            //};
        }

        public ResolvedSigningRequest resolve(AbiMap abis, EosSharp.Core.Api.v1.PermissionLevel signer, TransactionContext ctx) {
            EosSharp.Core.Api.v1.Transaction transaction = resolveTransaction(abis, signer, ctx);

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
                        //foreach(var key in deserializedData.Keys)
                        //{
                        //    if (deserializedData[key] is string stringValue)
                        //    {
                        //        if (stringValue == Constants.PlaceholderName)
                        //        {
                        //            deserializedData[key] = signer.actor;
                        //        }
                        //        else if (stringValue == Constants.PlaceholderPermission)
                        //        {
                        //            deserializedData[key] = signer.permission;
                        //        }
                        //    }
                        //}
                        
                        action.data = deserializedData;
                    }
                }
            }

            var serializedTransaction = _abiSerializationProvider.SerializePackedTransaction(transaction, abis);
            return new ResolvedSigningRequest(this, signer, transaction, serializedTransaction);
        }

        //private void ReplacePlaceholders(Dictionary<string, object> valueDictionary, PermissionLevel replacement)
        //{
        //    foreach (var key in valueDictionary.Keys)
        //    {
        //        if (valueDictionary[key] is string stringValue)
        //        {
        //            if (stringValue == Constants.PlaceholderName)
        //            {
        //                valueDictionary[key] = signer.actor;
        //            }
        //            else if (stringValue == Constants.PlaceholderPermission)
        //            {
        //                valueDictionary[key] = signer.permission;
        //            }
        //        }
        //    }
        //}

        /**
         * Get the id of the chain where this request is valid.
         * @returns The 32-byte chain id as hex encoded string.
         */
        public ChainId getChainId() {
            var id= data.chain_id;
            switch (id.Key)
            {
                case "chain_id":
                    return (string)id.Value;
                case "chain_alias":
                    if (Constants.ChainIdLookup.ContainsKey((ChainName)id.Value))
                    {
                        return Constants.ChainIdLookup[(ChainName)id.Value];
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
        public EosSharp.Core.Api.v1.Action[] getRawActions()
        {
            var req = this.data.req;
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
                                //new List<PermissionLevel>()
                                //{
                                //    auths
                                //    //new PermissionLevel()
                                //    //{
                                //    //    actor = (string)auth.FirstOrDefault(a => a.Key == "actor").Value,
                                //    //    permission = (string)auth.FirstOrDefault(a => a.Key == "permission").Value
                                //    //}
                                //},
                                name = (string)dict["name"],
                                account = (string)dict["account"]
                            }
                        };
                    }
                    else
                        throw new Exception("unsupported type for data.req");
                case "action[]":
                    return (EosSharp.Core.Api.v1.Action[]) req.Value;
                case "identity":
                    string data = "0101000000000000000200000000000000"; // placeholder permission
                    EosSharp.Core.Api.v1.PermissionLevel authorization = Constants.PlaceholderAuth;

                    if (req.Value is Dictionary<string, object> valueDict)
                    {
                        if (valueDict.TryGetValue("permission", out var permissionObj))
                        {
                            if (permissionObj != null)
                            {
                                // TODO
                                /*idType.serialize(buf, req.Item2);
                                data = SerializationHelper.ByteArrayToHexString(buf.asUint8Array());*/

                                // TODO serialize identity-request-type?
                                data = SerializationHelper.ByteArrayToHexString(new byte[] { });
// TODO
//                                authorization = ((IdentityV2)req.Value).permission;
                            }
                        }
//                        return permission == Constants.PlaceholderPermission ? null : permission;
                    }

                    return new[]
                    {
                        new EosSharp.Core.Api.v1.Action()
                        {
                            account = "",
                            name = "identity",
                            authorization = new List<EosSharp.Core.Api.v1.PermissionLevel>(){ authorization },
                            hex_data = data // TODO data or hex_data?
                        },
                    };
                case "transaction":
                    return ((EosSharp.Core.Api.v1.Transaction)req.Value).actions.ToArray();
                default:
                    throw new Exception("Invalid signing request data");
            }
        }

        /** Unresolved transaction. */
        public EosSharp.Core.Api.v1.Transaction getRawTransaction() {
            var req  = data.req;
            switch (req.Key)
            {
                case "transaction":
                    return (EosSharp.Core.Api.v1.Transaction)req.Value;
                case "action":
                case "action[]":
                case "identity":
                    return new EosSharp.Core.Api.v1.Transaction()
                    {
                        actions = getRawActions().ToList(),
                        context_free_actions = new List<EosSharp.Core.Api.v1.Action>(),
                        transaction_extensions = new List<EosSharp.Core.Api.v1.Extension>(),
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
        public bool isIdentity()
        {
            return data.req.Key == "identity";
        }

        /** Whether the request should be broadcast by signer. */
        public bool shouldBroadcast() {
            if (isIdentity())
            {
                return false;
            }

            return (data.flags & AbiConstants.RequestFlagsBroadcast) != 0;
        }

        /**
         * Present if the request is an identity request and requests a specific account.
         * @note This returns `nil` unless a specific identity has been requested,
         *       use `isIdentity` to check id requests.
         */
        public string getIdentity() {
            if (data.req.Key == "identity")
            {
                string actor = Constants.PlaceholderName;
                if (data.req.Value is Dictionary<string, object> valueDict)
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
                    return actor == Constants.PlaceholderName ? null : actor;
                }
            }
            return null;
        }

        /**
     * Present if the request is an identity request and requests a specific permission.
     * @note This returns `nil` unless a specific permission has been requested,
     *       use `isIdentity` to check id requests.
     */
        public string getIdentityPermission() {
            if (data.req.Key == "identity")
            {
                string permission = Constants.PlaceholderPermission;
                if (data.req.Value is Dictionary<string, object> valueDict)
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
                    return permission == Constants.PlaceholderPermission ? null : permission;
                }
            }
            return null;
        }

        /** Get raw info dict */
        public Dictionary<string, byte[]> getRawInfo()
        {
//            let rv: {[key: string]: Uint8Array } = { }
            var rv = new Dictionary<string, byte[]>();

            foreach (var info in data.info)
            {
                if(info is InfoPair infoPair )
                    rv.Add(infoPair.key, infoPair.value is string ? SerializationHelper.HexStringToByteArray((string)infoPair.value) : (byte[])infoPair.value);   // TODO 
                else if(info is Dictionary<string, object> infoDict)
                {
                    if(infoDict.ContainsKey("key") && infoDict.ContainsKey("value"))
                        rv.Add((string)infoDict["key"], infoDict["value"] is string ? SerializationHelper.HexStringToByteArray((string)infoDict["value"]) : (byte[])infoDict["value"]);   // TODO                         
                    else
                    {
                        throw new NotSupportedException("Dictionary without \"key\" and \"value\" not supported");
                    }
                }
            }
            return rv;
        }

        /** Get metadata values as T */
        public T getInfo<T>(string key, string abiSerializableType)
        {
            var rv = new Dictionary<string, string>();
            var raw = getRawInfo();
            if (raw.TryGetValue(key, out var rawVal))
            {
                return _abiSerializationProvider.Deserialize<T>(rawVal, abiSerializableType);
            }
            else
                throw new KeyNotFoundException($"Key {key} not found");
        }

        /** Get metadata values as T */
        public string getInfo(string key, string abiSerializableType = "string")
        {
            var raw = getRawInfo();
            if (raw.TryGetValue(key, out var rawVal))
            {
                return _abiSerializationProvider.Deserialize<string>(rawVal, abiSerializableType);
            }
            else
                throw new KeyNotFoundException($"Key {key} not found");
        }

        /** Get metadata values as strings. */
        public Dictionary<string, string> getInfos()
        {
            var rv = new Dictionary<string, string>();
            var raw = getRawInfo();

            foreach (var rawInfo in raw)
            {
                rv.Add(rawInfo.Key, _abiSerializationProvider.Deserialize<string>(rawInfo.Value, "string"));
            }

            return rv;
        }

        /** Set a metadata key. */
        public void setInfoKey(string key, object value, string abiSerializableType = null /* string | boolean*/)
        {
            var infoPairs = data.info.Cast<InfoPair>().ToList();
            var pair = infoPairs.SingleOrDefault(i => i.key == key); 
            
            byte[] encodedValue;
            if (abiSerializableType == null)
            {
                switch (value)
                {
                    case string stringtype:
                        encodedValue = Encoding.UTF8.GetBytes(stringtype);  // TODO UTF-8 ?
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
                    key = key,
                    value = encodedValue
                };
                data.info.Add(pair);
            }
            else
            {
                encodedValue = _abiSerializationProvider.Serialize(value, abiSerializableType);
                pair = new InfoPair()
                {
                    key = key,
                    value = encodedValue
                };
                data.info.Add(pair);
            }

        }

        /** Return a deep copy of this request. */
        public SigningRequest clone()
        {
            SigningRequestData clonedData = null;
            if(data != null)
                clonedData = new SigningRequestData()
                {
                    req = data.req,
                    callback = data.callback,
                    info = data.info,
                    chain_id = data.chain_id,
                    flags = data.flags
                };
            RequestSignature clonedRequestSignature = null;
            if (signature != null)
                clonedRequestSignature = new RequestSignature()
                {
                    signature = signature.signature,
                    signer = signature.signer
                };

            return new SigningRequest(
                this.version,
                clonedData,
                _zlib,
                _abiSerializationProvider,
                clonedRequestSignature
            );
        }

        // Convenience methods.

        /*public string toString()
        {
            return this.encode();
        }

        public object toJSON()  // TODO
        {
            return this.encode();
        }*/
        /**
         * Present if the request is an identity request and requests a specific permission.
         * @note This returns `nil` unless a specific permission has been requested,
         *       use `isIdentity` to check id requests.
         */
        public string getIdentityScope() {
            if (!this.isIdentity() || this.version <= 2)
            {
                return null;
            }

            var id = this.data.req.Value as IdentityV3;
            return id.scope;
        }
}

    public class ResolvedSigningRequest
    {
        /** Recreate a resolved request from a callback payload. */
        public static async Task<ResolvedSigningRequest> fromPayload(CallbackPayload payload, SigningRequestEncodingOptions options, IAbiSerializationProvider abiSerializationProvider) {
            SigningRequest request = SigningRequest.from(payload.req, options);
            var abis = await request.fetchAbis(abiSerializationProvider);
            return request.resolve(
                abis,
                new EosSharp.Core.Api.v1.PermissionLevel()
                {
                    actor = payload.sa,
                    permission = payload.sp
                },
                new TransactionContext()
                {
                    ref_block_num = Convert.ToUInt16(payload.rbn),
                    ref_block_prefix = Convert.ToUInt32(payload.rid),
                    expiration = Convert.ToDateTime(payload.ex)
                }
            );
        }

        public readonly SigningRequest request;
        public readonly EosSharp.Core.Api.v1.PermissionLevel signer;
        public readonly EosSharp.Core.Api.v1.Transaction transaction;
        public readonly byte[] serializedTransaction;

        public ResolvedSigningRequest(SigningRequest request, EosSharp.Core.Api.v1.PermissionLevel signer, EosSharp.Core.Api.v1.Transaction transaction, byte[] serializedTransaction)
        {
            this.request = request;
            this.signer = signer;
            this.transaction = transaction;
            this.serializedTransaction = serializedTransaction;
        }

        public string getTransactionId()
        {
            return SerializationHelper.ByteArrayToHexString(Sha256Manager.GetHash(serializedTransaction));
        }

        public ResolvedCallback getCallback(string[] signatures, int? blockNum)
        {

            string callback = request.data.callback;
            RequestFlags flags = request.data.flags;

            if (string.IsNullOrEmpty(callback))
            {
                return null;
            }

            if (signatures == null || signatures.Length == 0)
            {
                throw new Exception("Must have at least one signature to resolve callback");
            }

            CallbackPayload payload = new CallbackPayload()
            {
                sig = signatures[0],
                tx = getTransactionId(),
                rbn = transaction.ref_block_num.ToString(),
                rid = transaction.ref_block_prefix.ToString(),
                ex = transaction.expiration.ToString(),
                req = request.encode(),
                sa = signer.actor,
                sp = signer.permission,
            };
            /*for ( const [ n, sig] of signatures.slice(1).entries()) {
                payload[`sig${ n }`] = sig
            }*/
            if (blockNum != null)
            {
                payload.bn = blockNum.ToString();
            }

            string url = callback
                .Replace("{{sig}}", payload.sig)
                .Replace("{{tx}}", payload.tx)
                .Replace("{{rbn}}", payload.rbn)
                .Replace("{{rid}}", payload.rid)
                .Replace("{{ex}}", payload.ex)
                .Replace("{{req}}", payload.req)
                .Replace("{{sa}}", payload.sa)
                .Replace("{{sp}}", payload.sp)
                .Replace("{{bn}}", payload.bn);

            return new ResolvedCallback()
            {
                background = (flags & AbiConstants.RequestFlagsBackground) != 0,
                payload = payload,
                url = url
            };
        }
    }
}