using System.Collections.Generic;
using EosSharp.Core.Api.v1;

namespace EosioSigningRequest
{
    public static class SigningRequestAbi
    {
        //! ABI-Object containing diverse types, structs, variants and actions used with the SigningRequest Package 
        public static readonly Abi Abi = new Abi()
        {
            version = "eosio::abi/1.1",
            types = new List<AbiType>()
            {
                new AbiType()
                {
                    new_type_name = "account_name",
                    type = "name",
                },
                new AbiType()
                {
                    new_type_name = "action_name",
                    type = "name",
                },
                new AbiType()
                {
                    new_type_name = "permission_name",
                    type = "name",
                },
                new AbiType()
                {
                    new_type_name = "chain_alias",//
                    type = "uint8",
                },
                new AbiType()
                {
                    new_type_name = "chain_id",//
                    type = "checksum256",
                },
                new AbiType()
                {
                    new_type_name = "request_flags",
                    type = "uint8",
                },
            },
            structs = new List<AbiStruct>()
            {
                new AbiStruct()
                {
                    name = "permission_level",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "actor",
                            type = "account_name",
                        },
                        new AbiField()
                        {
                            name = "permission",
                            type = "permission_name",
                        }
                    }
                },
                new AbiStruct()
                {
                    name = "action",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "account",
                            type = "account_name",
                        },
                        new AbiField()
                        {
                            name = "name",
                            type = "action_name",
                        },
                        new AbiField()
                        {
                            name = "authorization",
                            type = "permission_level[]",
                        },
                        new AbiField()
                        {
                            name = "data",
                            type = "bytes",
                        }
                    }
                },
                new AbiStruct()
                {
                    name = "extension",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "type",
                            type = "uint16",
                        },
                        new AbiField()
                        {
                            name = "data",
                            type = "bytes",
                        }
                    }
                },
                new AbiStruct()
                {
                    name = "transaction_header",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "expiration",
                            type = "time_point_sec",
                        },
                        new AbiField()
                        {
                            name = "ref_block_num",
                            type = "uint16",
                        },
                        new AbiField()
                        {
                            name = "ref_block_prefix",
                            type = "uint32",
                        },
                        new AbiField()
                        {
                            name = "max_net_usage_words",
                            type = "varuint32",
                        },
                        new AbiField()
                        {
                            name = "max_cpu_usage_ms",
                            type = "uint8",
                        },
                        new AbiField()
                        {
                            name = "delay_sec",
                            type = "varuint32",
                        }
                    }
                },
                new AbiStruct()
                {
                    name = "transaction",
                    @base = "transaction_header",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "context_free_actions",
                            type = "action[]",
                        },
                        new AbiField()
                        {
                            name = "actions",
                            type = "action[]",
                        },
                        new AbiField()
                        {
                            name = "transaction_extensions",
                            type = "extension[]",
                        },
                    }
                },
                new AbiStruct()
                {
                    name = "info_pair",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "key",
                            type = "string",
                        },
                        new AbiField()
                        {
                            name = "value",
                            type = "bytes",
                        },
                    }
                },
                new AbiStruct()
                {
                    name = "signing_request",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "chain_id",
                            type = "variant_id",
                        },
                        new AbiField()
                        {
                            name = "req",
                            type = "variant_req",
                        },
                        new AbiField()
                        {
                            name = "flags",
                            type = "request_flags",
                        },
                        new AbiField()
                        {
                            name = "callback",
                            type = "string",
                        },
                        new AbiField()
                        {
                            name = "info",
                            type = "info_pair[]",
                        },
                    }
                },
                new AbiStruct()
                {
                    name = "signing_request_identity_v3",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "chain_id",
                            type = "variant_id",
                        },
                        new AbiField()
                        {
                            name = "req",
                            type = "variant_req_identity_v3",
                        },
                        new AbiField()
                        {
                            name = "flags",
                            type = "request_flags",
                        },
                        new AbiField()
                        {
                            name = "callback",
                            type = "string",
                        },
                        new AbiField()
                        {
                            name = "info",
                            type = "info_pair[]",
                        },
                    }
                },
                new AbiStruct()
                {
                    name = "identity",
                    fields = new List<AbiField>
                    {
                        new AbiField()
                        {
                            name = "permission",
                            type = "permission_level?"
                        }
                        //new AbiField()
                        //{
                        //    name = "account",
                        //    type = "name",
                        //},
                        //new AbiField()
                        //{
                        //    name = "request_key",
                        //    type = "public_key?",
                        //}
                    }
                },
                new AbiStruct()
                {
                    name = "identity_v3",
                    fields = new List<AbiField>
                    {
                        new AbiField()
                        {
                            name = "permission",
                            type = "permission_level?"
                        },
                        new AbiField()
                        {
                            name = "scope",
                            type = "name",
                        },
                        //new AbiField()
                        //{
                        //    name = "request_key",
                        //    type = "public_key?",
                        //}
                    }
                },
                new AbiStruct()
                {
                    name = "request_signature",
                    fields = new List<AbiField>()
                    {
                        new AbiField()
                        {
                            name = "signer",
                            type = "name",
                        },
                        new AbiField()
                        {
                            name = "signature",
                            type = "signature",
                        },
                    }
                }
            },
            variants = new List<Variant>()
            {
                new Variant()
                {
                    name = "variant_id",
                    types = new List<string> {"chain_alias", "chain_id"},
                },
                new Variant()
                {
                    name = "variant_req",
                    types = new List<string>() {"action", "action[]", "transaction", "identity"},
                }
                ,
                new Variant()
                {
                    name = "variant_req_identity_v3",
                    types = new List<string>() {"action", "action[]", "transaction", "identity_v3"},
                }
            },
            actions = new List<AbiAction>()
            {
                new AbiAction()
                {
                    name = "identity",
                    type = "identity",
                }
            }
        };
    }

}
