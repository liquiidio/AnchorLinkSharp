/** SigningRequest ABI and typedefs. */

using System;
using System.Collections.Generic;
using EosSharp.Core.DataAttributes;
using AccountName = System.String; /*name*/
using VariantId = System.Collections.Generic.KeyValuePair<string, object>;
using VariantReq = System.Collections.Generic.KeyValuePair<string, object>;
using RequestFlags = System.Byte;  //number;  // TODO

namespace EosioSigningRequest
{
    //public struct VariantId
    //{
    //    public string Key;

    //    public object Value;
    //}

    public static class AbiConstants
    {
        public static byte RequestFlagsNone = 0;
        public static byte RequestFlagsBroadcast = 1 << 0;
        public static byte RequestFlagsBackground = 1 << 1;
    }

    public class TransactionHeader
    {
        public DateTime? expiration; /*time_point_sec*/ // TODO
        public ushort? ref_block_num; /*uint16*/
        public uint? ref_block_prefix; /*uint32*/
        public uint? max_net_usage_words; /*varuint32*/
        public byte? max_cpu_usage_ms; /*uint8*/
        public uint? delay_sec; /*varuint32*/
    }

    public class SigningRequestData
    {
        public VariantId chain_id;
        public VariantReq req;
        public RequestFlags flags;
        public string callback;
        public List<object> info;
    }

    public class InfoPair
    {
        public InfoPair()
        {

        }

        public InfoPair(string key, object value)
        {
            this.key = key;
            this.value = value;
        }

        public string key;
        public object value; //: Uint8Array | string /*bytes*/ // TODO// byte[] or string
    }

    public class IdentityV2
    {
        public EosSharp.Core.Api.v1.PermissionLevel permission; // TODO | undefined | null
    }

    public class IdentityV3 : IdentityV2
    {
        public string scope;
    }

    public class RequestSignature
    {
        public AccountName signer;
        public string signature;
    }

// TODO
/*export const data = {
    version: 'eosio::abi/1.1',
    types: [
        {
            new_type_name: 'account_name',
            type: 'name',
        },
        {
            new_type_name: 'action_name',
            type: 'name',
        },
        {
            new_type_name: 'permission_name',
            type: 'name',
        },
        {
            new_type_name: 'chain_alias',
            type: 'uint8',
        },
        {
            new_type_name: 'chain_id',
            type: 'checksum256',
        },
        {
            new_type_name: 'request_flags',
            type: 'uint8',
        },
    ],
    structs: [
        {
            name: 'permission_level',
            fields: [
                {
                    name: 'actor',
                    type: 'account_name',
                },
                {
                    name: 'permission',
                    type: 'permission_name',
                },
            ],
        },
        {
            name: 'action',
            fields: [
                {
                    name: 'account',
                    type: 'account_name',
                },
                {
                    name: 'name',
                    type: 'action_name',
                },
                {
                    name: 'authorization',
                    type: 'permission_level[]',
                },
                {
                    name: 'data',
                    type: 'bytes',
                },
            ],
        },
        {
            name: 'extension',
            fields: [
                {
                    name: 'type',
                    type: 'uint16',
                },
                {
                    name: 'data',
                    type: 'bytes',
                },
            ],
        },
        {
            name: 'transaction_header',
            fields: [
                {
                    name: 'expiration',
                    type: 'time_point_sec',
                },
                {
                    name: 'ref_block_num',
                    type: 'uint16',
                },
                {
                    name: 'ref_block_prefix',
                    type: 'uint32',
                },
                {
                    name: 'max_net_usage_words',
                    type: 'varuint32',
                },
                {
                    name: 'max_cpu_usage_ms',
                    type: 'uint8',
                },
                {
                    name: 'delay_sec',
                    type: 'varuint32',
                },
            ],
        },
        {
            name: 'transaction',
            base: 'transaction_header',
            fields: [
                {
                    name: 'context_free_actions',
                    type: 'action[]',
                },
                {
                    name: 'actions',
                    type: 'action[]',
                },
                {
                    name: 'transaction_extensions',
                    type: 'extension[]',
                },
            ],
        },
        {
            name: 'info_pair',
            fields: [
                {
                    name: 'key',
                    type: 'string',
                },
                {
                    name: 'value',
                    type: 'bytes',
                },
            ],
        },
        {
            name: 'signing_request',
            fields: [
                {
                    name: 'chain_id',
                    type: 'variant_id',
                },
                {
                    name: 'req',
                    type: 'variant_req',
                },
                {
                    name: 'flags',
                    type: 'request_flags',
                },
                {
                    name: 'callback',
                    type: 'string',
                },
                {
                    name: 'info',
                    type: 'info_pair[]',
                },
            ],
        },
        {
            name: 'identity',
            fields: [
                {
                    name: 'permission',
                    type: 'permission_level?',
                },
            ],
        },
        {
            name: 'request_signature',
            fields: [
                {
                    name: 'signer',
                    type: 'name',
                },
                {
                    name: 'signature',
                    type: 'signature',
                },
            ],
        },
    ],
    variants: [
        {
            name: 'variant_id',
            types: ['chain_alias', 'chain_id'],
        },
        {
            name: 'variant_req',
            types: ['action', 'action[]', 'transaction', 'identity'],
        },
    ],
    actions: [
        {
            name: 'identity',
            type: 'identity',
        },
    ],
}*/
}